// HTMX requests that aren't <form> submits (bare hx-post/hx-delete buttons) don't carry the
// antiforgery hidden field a <form> would, so attach it as a header on every htmx request instead.
// Program.cs configures AntiforgeryOptions.HeaderName to match, so [ValidateAntiForgeryToken] accepts it.
document.body.addEventListener('htmx:configRequest', (event) => {
    const token = document.querySelector('meta[name="csrf-token"]')?.content;
    if (token) {
        event.detail.headers['X-CSRF-TOKEN'] = token;
    }
});
