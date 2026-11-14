(() => {
    "use strict";

    const ui = window.AdminUI;
    if (!ui) return;
    const initialized = new WeakSet();
    let data;
    let modal;
    let loadSequence = 0;

    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    const request = (url, method = "GET", body) => ui.getJson(url, {
        method,
        headers: body ? { "Content-Type": "application/json", RequestVerificationToken: token() } : {},
        ...(body ? { body: JSON.stringify(body) } : {})
    });
    const byId = (rows, id) => rows.find(row => row.id === id);
    const faDate = value => value ? new Intl.DateTimeFormat("fa-IR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)) : "—";
    const el = (tag, text, className) => {
        const node = document.createElement(tag);
        if (text != null) node.textContent = text;
        if (className) node.className = className;
        return node;
    };

    async function load(page) {
        if (page.isConnected === false) return;
        const sequence = ++loadSequence;
        const status = page.querySelector("[data-users-status]");
        status.classList.remove("text-danger"); status.classList.add("text-muted", "small");
        status.textContent = "در حال بارگذاری…";
        try {
            const result = await request(`${page.dataset.api}?section=${encodeURIComponent(page.dataset.adminUsersPage || "users")}`);
            if (sequence !== loadSequence || page.isConnected === false) return;
            data = result;
            if (page.dataset.adminUsersPage === "users") renderUsers(page);
            if (page.dataset.adminUsersPage === "permissions") renderRoles(page);
            if (page.dataset.adminUsersPage === "roles") renderRoleManagement(page);
            status.textContent = "";
        } catch (error) {
            if (sequence !== loadSequence || page.isConnected === false) return;
            status.textContent = error.message;
            status.classList.remove("text-muted"); status.classList.add("text-danger", "small");
        }
    }

    function renderUsers(page) {
        const body = page.querySelector("[data-users-rows]");
        ui.destroyDataTable(page.querySelector("[data-admin-datatable]"));
        body.replaceChildren();
        for (const user of data.users) {
            const row = document.createElement("tr");
            const name = el("td");
            name.append(el("strong", user.displayName), el("small", user.userName, "d-block text-muted"));
            const roles = el("td", user.roleIds.map(id => byId(data.roles, id)?.name).filter(Boolean).join("، ") || "بدون نقش");
            const warehouses = el("td");
            const isFull = user.roleIds.some(id => byId(data.roles, id)?.roleKey === "system.full-access");
            warehouses.textContent = isFull ? "همهٔ انبارها" : user.warehouseIds.map(id => byId(data.warehouses, id)?.name).filter(Boolean).join("، ") || "بدون دسترسی";
            const state = el("td");
            const badge = el("span", user.isActive ? "فعال" : "غیرفعال", `badge ${user.isActive ? "bg-success" : "bg-secondary"}`);
            state.append(badge);
            const login = el("td", faDate(user.lastLoginAtUtc));
            const actions = el("td", null, "text-nowrap");
            const edit = el("button", "ویرایش", "btn btn-sm btn-default me-1");
            edit.type = "button";
            ui.withIcon(edit, "fa-pen-to-square");
            edit.addEventListener("click", () => openUserModal(page, user));
            const reset = el("button", "گذرواژه", "btn btn-sm btn-outline-secondary");
            reset.type = "button";
            ui.withIcon(reset, "fa-key");
            reset.addEventListener("click", () => openPasswordModal(page, user));
            actions.append(edit, reset);
            row.append(name, roles, warehouses, state, login, actions);
            body.append(row);
        }
        if (!data.users.length) {
            const row = document.createElement("tr");
            const cell = el("td", "کاربری ثبت نشده است.");
            cell.colSpan = 6;
            row.append(cell);
            body.append(row);
        }
        ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
    }

    function addCheck(container, name, id, label, checked, depth = 0) {
        const wrapper = el("div", null, "col-md-6 col-xl-4");
        const check = el("label", null, "form-check d-flex gap-2 align-items-start");
        check.style.marginInlineStart = `${depth * 1.25}rem`;
        const input = document.createElement("input");
        input.className = "form-check-input";
        input.type = "checkbox";
        input.name = name;
        input.value = String(id);
        input.checked = checked;
        check.append(input, el("span", label, "form-check-label"));
        wrapper.append(check);
        container.append(wrapper);
    }

    function openUserModal(page, user = null) {
        const form = document.querySelector("[data-user-form]");
        form.reset();
        form.elements.id.value = user?.id || "";
        form.elements.userName.value = user?.userName || "";
        form.elements.displayName.value = user?.displayName || "";
        form.elements.isActive.checked = user?.isActive ?? true;
        form.querySelector("[data-new-password]").classList.toggle("d-none", !!user);
        form.elements.password.required = !user;
        document.getElementById("admin-user-modal-title").textContent = user ? "ویرایش کاربر سیستم" : "افزودن کاربر سیستم";
        const roles = form.querySelector("[data-user-roles]");
        const warehouses = form.querySelector("[data-user-warehouses]");
        roles.replaceChildren();
        warehouses.replaceChildren();
        for (const role of data.roles.filter(item => item.isActive))
            addCheck(roles, "roleIds", role.id, role.name, user?.roleIds.includes(role.id) || false);
        for (const warehouse of data.warehouses)
            addCheck(warehouses, "warehouseIds", warehouse.id, `${warehouse.name} (${warehouse.code})`, user?.warehouseIds.includes(warehouse.id) || false);
        modal.show();
    }

    function renderRoles(page) {
        const select = page.querySelector("[data-role-select]");
        const previousRole = select.value;
        select.replaceChildren();
        for (const role of data.roles) {
            const option = el("option", role.name);
            option.value = String(role.id);
            option.dataset.roleKey = role.roleKey;
            select.append(option);
        }
        if (data.roles.some(role => String(role.id) === previousRole)) select.value = previousRole;
        renderPermissions(page);
    }

    function renderPermissions(page) {
        const tree = page.querySelector("[data-permission-tree]");
        const roleId = Number(page.querySelector("[data-role-select]").value);
        const role = byId(data.roles, roleId);
        const save = page.querySelector("[data-role-save]");
        save.disabled = !role || role.roleKey === "system.full-access";
        tree.replaceChildren();
        const childrenByParent = new Map();
        for (const permission of data.permissions) {
            const parentKey = permission.parentKey || "";
            if (!childrenByParent.has(parentKey)) childrenByParent.set(parentKey, []);
            childrenByParent.get(parentKey).push(permission);
        }
        const children = parentKey => childrenByParent.get(parentKey || "") || [];
        const effectivePermissions = new Set(role?.permissionIds || []);
        const includeDescendants = permissionKey => {
            for (const child of children(permissionKey)) {
                effectivePermissions.add(child.id);
                includeDescendants(child.permissionKey);
            }
        };
        for (const selected of data.permissions.filter(item => effectivePermissions.has(item.id)))
            includeDescendants(selected.permissionKey);
        const hasSelectedDescendant = permissionKey => children(permissionKey).some(child =>
            effectivePermissions.has(child.id) || hasSelectedDescendant(child.permissionKey));
        const inputsByPermissionKey = new Map();
        const appendChildren = parentKey => {
            const list = document.createElement("ul");
            list.className = "admin-permission-list";
            list.setAttribute("role", "group");
            for (const permission of children(parentKey)) {
                const descendants = children(permission.permissionKey);
                const node = el("li", null, "admin-permission-node");
                node.setAttribute("role", "treeitem");
                const row = el("div", null, "admin-permission-row");
                const childList = descendants.length ? appendChildren(permission.permissionKey) : null;
                const expanded = Boolean(childList && hasSelectedDescendant(permission.permissionKey));
                if (childList) {
                    const toggle = el("button", null, "admin-permission-toggle");
                    toggle.type = "button";
                    toggle.setAttribute("aria-expanded", String(expanded));
                    toggle.setAttribute("aria-label", `${expanded ? "بستن" : "بازکردن"} ${permission.name}`);
                    toggle.append(el("span", "›", "admin-permission-chevron"));
                    toggle.addEventListener("click", () => {
                        const open = toggle.getAttribute("aria-expanded") !== "true";
                        toggle.setAttribute("aria-expanded", String(open));
                        toggle.setAttribute("aria-label", `${open ? "بستن" : "بازکردن"} ${permission.name}`);
                        node.setAttribute("aria-expanded", String(open));
                        childList.hidden = !open;
                    });
                    row.append(toggle);
                    node.setAttribute("aria-expanded", String(expanded));
                    childList.hidden = !expanded;
                } else {
                    row.append(el("span", null, "admin-permission-toggle-spacer"));
                }
                const label = el("label", null, "admin-permission-choice");
                const input = document.createElement("input");
                input.type = "checkbox";
                input.className = "admin-permission-checkbox";
                input.value = String(permission.id);
                input.checked = effectivePermissions.has(permission.id);
                input.disabled = role?.roleKey === "system.full-access";
                inputsByPermissionKey.set(permission.permissionKey, input);
                input.addEventListener("change", () => {
                    input.indeterminate = false;
                    const setDescendants = (permissionKey, checked) => {
                        for (const child of children(permissionKey)) {
                            const childInput = inputsByPermissionKey.get(child.permissionKey);
                            if (childInput) {
                                childInput.checked = checked;
                                childInput.indeterminate = false;
                            }
                            setDescendants(child.permissionKey, checked);
                        }
                    };
                    setDescendants(permission.permissionKey, input.checked);

                    let parentKey = permission.parentKey;
                    while (parentKey) {
                        const parentInput = inputsByPermissionKey.get(parentKey);
                        const siblingInputs = children(parentKey)
                            .map(child => inputsByPermissionKey.get(child.permissionKey))
                            .filter(Boolean);
                        if (parentInput && siblingInputs.length) {
                            const allSelected = siblingInputs.every(child => child.checked && !child.indeterminate);
                            const someSelected = siblingInputs.some(child => child.checked || child.indeterminate);
                            parentInput.checked = allSelected;
                            parentInput.indeterminate = someSelected && !allSelected;
                        }
                        parentKey = data.permissions.find(item => item.permissionKey === parentKey)?.parentKey;
                    }
                });
                label.append(input, el("span", permission.name, "admin-permission-name"));
                row.append(label);
                node.append(row);
                if (childList) node.append(childList);
                list.append(node);
            }
            return list;
        };
        tree.setAttribute("role", "tree");
        tree.setAttribute("aria-label", "مجوزهای نقش");
        tree.replaceChildren(appendChildren(null));

        const refreshParentStates = parentKey => {
            for (const permission of children(parentKey)) refreshParentStates(permission.permissionKey);
            if (!parentKey) return;
            const parentInput = inputsByPermissionKey.get(parentKey);
            const childInputs = children(parentKey)
                .map(child => inputsByPermissionKey.get(child.permissionKey))
                .filter(Boolean);
            if (!parentInput || !childInputs.length) return;
            const allSelected = childInputs.every(child => child.checked && !child.indeterminate);
            const someSelected = childInputs.some(child => child.checked || child.indeterminate);
            parentInput.checked = allSelected;
            parentInput.indeterminate = someSelected && !allSelected;
        };
        refreshParentStates("");
    }

    function renderRoleManagement(page) {
        const body = page.querySelector("[data-role-rows]");
        ui.destroyDataTable(page.querySelector("[data-admin-datatable]"));
        body.replaceChildren();
        for (const role of data.roles) {
            const row = document.createElement("tr");
            row.append(
                el("td", role.name),
                el("td", role.roleKey, "font-monospace"),
                el("td", new Intl.NumberFormat("fa-IR").format(role.userCount ?? data.users.filter(user => user.roleIds.includes(role.id)).length)),
                el("td", role.isActive ? "فعال" : "غیرفعال")
            );
            const actions = el("td", null, "text-nowrap");
            const edit = el("button", "ویرایش", "btn btn-sm btn-default");
            edit.type = "button";
            edit.dataset.roleEdit = String(role.id);
            actions.append(edit);
            row.append(actions);
            body.append(row);
        }
        ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
    }

    function openPasswordModal(page, user) {
        const old = document.getElementById("admin-password-modal");
        old?.remove();
        const wrapper = document.createElement("div");
        wrapper.innerHTML = `<div class="modal fade" id="admin-password-modal" tabindex="-1" aria-labelledby="admin-password-title" aria-hidden="true"><div class="modal-dialog"><form class="modal-content" data-password-form><div class="modal-header"><h2 class="modal-title fs-5" id="admin-password-title">تغییر گذرواژه: ${escapeHtml(user.userName)}</h2><button type="button" class="btn-close ms-0" data-bs-dismiss="modal" aria-label="بستن"></button></div><div class="modal-body"><label class="form-label" for="admin-new-password">گذرواژهٔ جدید</label><input class="form-control" id="admin-new-password" name="password" type="password" maxlength="200" autocomplete="new-password" required /><small class="form-text">هر گذرواژهٔ غیرخالی تا ۲۰۰ نویسه پذیرفته می‌شود.</small></div><div class="modal-footer"><button type="button" class="btn btn-default" data-bs-dismiss="modal">انصراف</button><button class="btn btn-primary" type="submit">ذخیرهٔ گذرواژه</button></div></form></div></div>`;
        const node = wrapper.firstElementChild;
        document.body.append(node);
        const passwordModal = window.bootstrap.Modal.getOrCreateInstance(node);
        node.querySelector("[data-password-form]").addEventListener("submit", async event => {
            event.preventDefault();
            const form = event.currentTarget;
            if (form.dataset.saving === "true") return;
            const password = new FormData(form).get("password");
            const submit = form.querySelector('[type="submit"]');
            form.dataset.saving = "true"; submit.disabled = true;
            try {
                const result = await request(`${page.dataset.api}/${user.id}/reset-password`, "POST", { password });
                ui.notify(result.message, "success");
                passwordModal.hide();
            } catch (error) { ui.notify(error.message, "danger"); }
            finally { form.dataset.saving = "false"; submit.disabled = false; }
        });
        node.addEventListener("hidden.bs.modal", () => node.remove(), { once: true });
        passwordModal.show();
    }

    function escapeHtml(value) {
        const node = document.createElement("span");
        node.textContent = value;
        return node.innerHTML;
    }

    function initialize(root) {
        const page = root.querySelector?.("[data-admin-users]");
        if (!page || initialized.has(page)) return;
        initialized.add(page);
        data = undefined;
        const mode = page.dataset.adminUsersPage || "users";
        const roleSelect = page.querySelector("[data-role-select]");
        if (mode === "users") {
            modal = window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("admin-user-modal"));
            page.querySelector("[data-user-create]").addEventListener("click", () => { if (data) openUserModal(page); else ui.notify("اطلاعات کاربران هنوز بارگذاری نشده است.", "danger"); });
        }
        if (roleSelect) roleSelect.addEventListener("change", () => { if (data) renderPermissions(page); });
        page.querySelector("[data-role-save]")?.addEventListener("click", async event => {
            const button = event.currentTarget;
            if (button.disabled) return;
            const roleId = Number(roleSelect.value);
            const role = byId(data?.roles || [], roleId);
            if (!role) { ui.notify("نقش را انتخاب کنید.", "danger"); return; }
            const permissionIds = [...page.querySelectorAll("[data-permission-tree] input:checked")].map(input => Number(input.value));
            button.disabled = true;
            try {
                const result = await request(`${page.dataset.api}/roles/${roleId}?section=permissions`, "PUT", {
                    name: role.name, isActive: role.isActive, permissionIds
                });
                ui.notify(result.message, "success");
                await load(page);
            } catch (error) { ui.notify(error.message, "danger"); }
            finally { button.disabled = false; }
        });
        page.querySelector("[data-role-create-form]")?.addEventListener("submit", async event => {
            event.preventDefault();
            const form = event.currentTarget;
            if (form.dataset.saving === "true") return;
            if (!form.elements.name.value.trim()) { ui.notify("نام نقش را وارد کنید.", "danger"); form.elements.name.focus(); return; }
            const submit = form.querySelector('[type="submit"]');
            form.dataset.saving = "true"; submit.disabled = true;
            try {
                const result = await request(`${page.dataset.api}/roles`, "POST", {
                    roleKey: form.elements.roleKey.value, name: form.elements.name.value
                });
                ui.notify(result.message, "success");
                form.reset();
                await load(page);
            } catch (error) { ui.notify(error.message, "danger"); }
            finally { form.dataset.saving = "false"; submit.disabled = false; }
        });
        page.querySelector("[data-role-rows]")?.addEventListener("click", event => {
            const button = event.target.closest("[data-role-edit]");
            const role = button && byId(data?.roles || [], Number(button.dataset.roleEdit));
            if (!role) return;
            const editor = page.querySelector("[data-role-editor]");
            const form = page.querySelector("[data-role-edit-form]");
            form.elements.id.value = String(role.id);
            form.elements.name.value = role.name;
            form.elements.name.disabled = role.roleKey === "system.full-access";
            form.elements.isActive.checked = role.isActive;
            form.elements.isActive.disabled = role.roleKey === "system.full-access";
            editor.hidden = false;
            editor.scrollIntoView({ behavior: "smooth", block: "center" });
        });
        page.querySelector("[data-role-edit-form]")?.addEventListener("submit", async event => {
            event.preventDefault();
            const form = event.currentTarget;
            if (form.dataset.saving === "true" || !data) return;
            if (!form.elements.name.value.trim()) { ui.notify("نام نقش را وارد کنید.", "danger"); form.elements.name.focus(); return; }
            const role = byId(data.roles, Number(form.elements.id.value));
            if (!role) return;
            const submit = form.querySelector('[type="submit"]');
            form.dataset.saving = "true"; submit.disabled = true;
            try {
                const result = await request(`${page.dataset.api}/roles/${role.id}?section=roles`, "PUT", {
                    name: form.elements.name.value,
                    isActive: form.elements.isActive.checked
                });
                ui.notify(result.message, "success");
                page.querySelector("[data-role-editor]").hidden = true;
                await load(page);
            } catch (error) { ui.notify(error.message, "danger"); }
            finally { form.dataset.saving = "false"; submit.disabled = false; }
        });
        page.querySelector("[data-user-form]")?.addEventListener("submit", async event => {
            event.preventDefault();
            const form = event.currentTarget;
            if (form.dataset.saving === "true") return;
            for (const [name, label] of [["userName", "نام کاربری"], ["displayName", "نام نمایشی"]]) {
                if (!form.elements[name].value.trim()) { ui.notify(`${label} را وارد کنید.`, "danger"); form.elements[name].focus(); return; }
            }
            if (form.elements.password.required && form.elements.password.value.length === 0) {
                ui.notify("گذرواژهٔ اولیه را وارد کنید.", "danger"); form.elements.password.focus(); return;
            }
            const selected = name => [...form.querySelectorAll(`[name="${name}"]:checked`)].map(input => Number(input.value));
            const userId = Number(form.elements.id.value) || null;
            const body = {
                id: userId,
                userName: form.elements.userName.value,
                displayName: form.elements.displayName.value,
                password: form.elements.password.value || null,
                isActive: form.elements.isActive.checked,
                roleIds: selected("roleIds"),
                warehouseIds: selected("warehouseIds")
            };
            const submit = form.querySelector('[type="submit"]');
            form.dataset.saving = "true"; submit.disabled = true;
            try {
                const result = await request(page.dataset.api, "POST", body);
                ui.notify(result.message, "success");
                modal.hide();
                await load(page);
            } catch (error) { ui.notify(error.message, "danger"); }
            finally { form.dataset.saving = "false"; submit.disabled = false; }
        });
        void load(page);
    }

    window.AdminUsers = Object.freeze({ initialize });
})();
