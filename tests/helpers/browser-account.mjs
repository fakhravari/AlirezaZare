// UI-only account fixture. Real authentication and persistence are covered by
// account-session.spec.mjs; these routes isolate layout and dialog tests.
export async function useBrowserAccount(page, {addressId = 'sample-address', selectedAddress = false, favorites = [], compare = [], cart = []} = {}) {
  const state = {
    email: 'layout@example.test', firstName: 'کاربر', lastName: 'آزمایشی', phone: '09123456789',
    addresses: addressId ? [{id: addressId, title: 'خانه', recipient: 'کاربر آزمایشی', phone: '09123456789',
      countryId: 1, provinceId: 1, cityId: 1, province: 'تهران', city: 'تهران', street: 'خیابان نمونه', postal: '1234567890'}] : [],
    cart, favorites: favorites.map(id => ({id})), compare: compare.map(id => ({id})), priceAlerts: [], articleInteractions: [], articleFeedback: [],
    orders: [{id: 1, number: 'TEST-1', status: 'pending-payment', orderedAtUtc: '2026-11-01T00:00:00Z', itemCount: 1, totalAmount: 1000}],
    checkout: selectedAddress ? {selectedAddressId: addressId, paymentMethod: 'zarinpal'} : null,
  };
  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path === '/api/auth/email-code/session') return route.fulfill({json: {authenticated: true}});
    if (path === '/api/account/state') return route.fulfill({json: state});
    if (path === '/api/account/cart' && request.method() === 'PUT') {
      state.cart = (request.postDataJSON()?.items || []).map(item => ({...item, id: item.productId}));
      return route.fulfill({json: {saved: true}});
    }
    return route.fallback();
  });
  return state;
}
