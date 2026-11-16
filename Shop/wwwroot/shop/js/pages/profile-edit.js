      (() => {
        const dialog = document.getElementById('profileEditDialog');
        const form = document.getElementById('profileEditModalForm');
        if (!dialog || !form) return;
        const storage = window.HNUI?.storage;
        const savedProfile = storage?.json('hn-profile', {}) || {};
        const phone = form.elements.phone;
        const currentPasswordField = form.querySelector('.profile-edit-password-current');
        const setGender = (idGender, legacyCode) => {
          const options = [...form.querySelectorAll('[name="idGender"]')];
          const selected = options.find(option => Number(option.value) === Number(idGender))
            || options.find(option => legacyCode && option.dataset.genderCode === legacyCode);
          if (selected) selected.checked = true;
        };
        const normalizePhone = value => window.HNUI.toEnglishDigits(value).replace(/\D/g, '').slice(0, 11);
        ['firstName', 'lastName', 'email'].forEach(name => {
          if (savedProfile[name]) form.elements[name].value = savedProfile[name];
        });
        if (savedProfile.phone) phone.value = normalizePhone(savedProfile.phone);
        setGender(savedProfile.idGender, savedProfile.gender);
        form.elements.newsletter.checked = savedProfile.newsletter === true;
        const setPasswordFields = hasPassword => {
          currentPasswordField.hidden = !hasPassword;
          document.getElementById('profilePasswordHint').textContent = hasPassword
            ? 'برای تغییر رمز، رمز فعلی و رمز جدید را وارد کنید.'
            : 'این حساب هنوز رمز ندارد؛ برای تعیین رمز، رمز جدید و تکرار آن را وارد کنید.';
        };
        setPasswordFields(savedProfile.hasPassword === true);
        const renderProfile = profile => {
          const name = `${profile.firstName || form.elements.firstName.value} ${profile.lastName || form.elements.lastName.value}`.trim();
          document.querySelector('[data-profile-value="name"]').textContent = name || 'ثبت نشده';
          document.querySelector('[data-profile-value="email"]').textContent = profile.email || form.elements.email.value || '—';
          document.querySelector('[data-profile-value="phone"]').textContent = window.HNUI.toPersianDigits(profile.phone || phone.value) || '—';
          const profileName = document.querySelector('.account-profile > div strong');
          if (profileName) profileName.textContent = name || 'حساب کاربری';
          document.querySelector('[data-profile-value="newsletter"]').textContent = profile.newsletter ? 'بله' : 'خیر';
        };
        renderProfile(savedProfile);
        const hydrateProfile = async () => {
          const response = await window.HNUI.api.request('account/state');
          if (!response.ok) return;
          const account = await response.json();
          Object.assign(savedProfile, { firstName: account.firstName, lastName: account.lastName, email: account.email, phone: account.phone, idGender: account.idGender, newsletter: account.newsletter, hasPassword: account.hasPassword === true });
          ['firstName', 'lastName'].forEach(name => { form.elements[name].value = savedProfile[name] || ''; });
          form.elements.email.value = account.email || '';
          if (savedProfile.phone) phone.value = normalizePhone(savedProfile.phone);
          form.elements.newsletter.checked = savedProfile.newsletter === true;
          setPasswordFields(savedProfile.hasPassword);
          setGender(savedProfile.idGender, savedProfile.gender);
          storage?.set('hn-profile', JSON.stringify(savedProfile)); renderProfile(savedProfile);
        };
        if (window.HNUI?.auth.isLoggedIn()) void hydrateProfile().catch(() => {});
        document.addEventListener('hn:auth-change', event => { if (event.detail) void hydrateProfile().catch(() => {}); });
        const profileName = document.querySelector('.account-profile > div strong');
        if (profileName) {
          profileName.textContent = `${form.elements.firstName.value.trim()} ${form.elements.lastName.value.trim()}`.trim();
        }
        const setError = (name, message) => {
          const host = form.querySelector(`[data-profile-edit-error="${name}"]`);
          const control = form.querySelector(name === 'gender' ? '[name="idGender"]' : `[name="${name}"]`);
          host.textContent = message;
          host.className = `profile-edit-validation ${message ? 'field-validation-error' : 'field-validation-valid'}`;
          if (name === 'gender') form.querySelector('.profile-edit-gender').classList.toggle('is-invalid', Boolean(message));
          else {
            control.classList.toggle('input-validation-error', Boolean(message));
            control.setAttribute('aria-invalid', String(Boolean(message)));
          }
        };
        const validate = () => {
          const firstName = form.elements.firstName.value.trim();
          const lastName = form.elements.lastName.value.trim();
          const email = form.elements.email;
          const normalizedPhone = normalizePhone(phone.value);
          const gender = form.querySelector('[name="idGender"]:checked');
          setError('firstName', firstName ? '' : 'نام را وارد کنید.');
          setError('lastName', lastName ? '' : 'نام خانوادگی را وارد کنید.');
          setError('email', !email.value || email.validity.valid ? '' : 'ایمیل را با قالب صحیح وارد کنید.');
          setError('phone', /^09\d{9}$/.test(normalizedPhone) ? '' : 'شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.');
          setError('gender', gender ? '' : 'جنسیت را انتخاب کنید.');
          return firstName && lastName && (!email.value || email.validity.valid) && /^09\d{9}$/.test(normalizedPhone) && gender;
        };
        document.querySelectorAll('[data-profile-edit-trigger]').forEach(trigger => trigger.addEventListener('click', event => {
          event.preventDefault();
          if (!window.HNUI?.auth.isLoggedIn()) { dialog.showModal(); return; }
          trigger.setAttribute('aria-busy', 'true');
          void hydrateProfile().catch(() => {}).finally(() => { trigger.removeAttribute('aria-busy'); dialog.showModal(); });
        }));
        document.querySelectorAll('[data-profile-edit-close]').forEach(close => close.addEventListener('click', () => dialog.close()));
        phone.addEventListener('input', () => {
          const normalizedPhone = normalizePhone(phone.value);
          if (phone.value !== normalizedPhone) phone.value = normalizedPhone;
        });
        form.addEventListener('input', validate);
        form.addEventListener('change', validate);
        form.addEventListener('submit', async event => {
          event.preventDefault();
          if (!validate()) return;
          const currentPassword = form.elements.currentPassword.value;
          const newPassword = form.elements.newPassword.value;
          const newPasswordConfirmation = form.elements.newPasswordConfirmation.value;
          const changingPassword = Boolean(currentPassword || newPassword || newPasswordConfirmation);
          if (changingPassword && (!newPassword || newPassword !== newPasswordConfirmation || (savedProfile.hasPassword && !currentPassword))) {
            document.getElementById('accountStatus').textContent = !newPassword
              ? 'رمز جدید را وارد کنید.'
              : newPassword !== newPasswordConfirmation
                ? 'تکرار رمز جدید مطابقت ندارد.'
                : 'برای تغییر رمز، رمز فعلی را وارد کنید.';
            return;
          }
          const profile = {
            firstName: form.elements.firstName.value.trim(),
            lastName: form.elements.lastName.value.trim(),
            email: form.elements.email.value.trim(),
            phone: normalizePhone(phone.value),
            idGender: Number(form.querySelector('[name="idGender"]:checked').value),
            newsletter: form.elements.newsletter.checked
          };
          if (window.HNUI?.auth.isLoggedIn()) {
              if (changingPassword) {
                const passwordResponse = await window.HNUI.api.request('account/password', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ currentPassword: currentPassword || null, newPassword, newPasswordConfirmation }) });
                if (!passwordResponse.ok) {
                  const result = await passwordResponse.json().catch(() => ({}));
                  document.getElementById('accountStatus').textContent = result.message || 'تغییر رمز عبور انجام نشد.';
                  return;
                }
                savedProfile.hasPassword = true;
                setPasswordFields(true);
                form.elements.currentPassword.value = '';
                form.elements.newPassword.value = '';
                form.elements.newPasswordConfirmation.value = '';
                document.getElementById('profilePasswordHint').textContent = 'برای تغییر رمز، رمز فعلی و رمز جدید را وارد کنید.';
              }
              const response = await window.HNUI.api.request('account/profile', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ firstName: profile.firstName, lastName: profile.lastName, phone: profile.phone, idGender: profile.idGender, newsletter: profile.newsletter }) });
            if (!response.ok) {
              const result = await response.json().catch(() => ({}));
              document.getElementById('accountStatus').textContent = result.message || 'ذخیرهٔ اطلاعات حساب انجام نشد.';
              return;
            }
            if (changingPassword) {
              profile.hasPassword = true;
            }
          }
          if (!storage?.set('hn-profile', JSON.stringify(profile))) return;
          const displayName = document.querySelector('.account-profile > div strong');
          if (displayName) displayName.textContent = `${profile.firstName} ${profile.lastName}`;
          renderProfile(profile);
          document.getElementById('accountStatus').textContent = 'اطلاعات پروفایل ذخیره شد.';
          dialog.close();
        });
      })();
