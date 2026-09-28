(function () {
    const listEl = document.getElementById("staff-notif-list");
    const badgeEl = document.getElementById("staff-notif-badge");
    const emptyEl = document.getElementById("staff-notif-empty");
    const toastHost = document.getElementById("staff-live-toasts");
    if (!listEl || !badgeEl) return;

    const items = new Map();
    const antiforgery = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

    function detailsUrl(applicationId) {
        return "/Applications/Details/" + applicationId;
    }

    function updateBadge() {
        const count = items.size;
        badgeEl.textContent = String(count);
        badgeEl.classList.toggle("d-none", count === 0);
        if (emptyEl) emptyEl.classList.toggle("d-none", count > 0);
    }

    function renderItem(n) {
        const a = document.createElement("a");
        a.href = detailsUrl(n.applicationId);
        a.className = "dropdown-item staff-notif-item";
        a.dataset.id = n.id;
        a.innerHTML =
            '<div class="fw-semibold">' + escapeHtml(n.title) + "</div>" +
            '<div class="small text-muted">' + escapeHtml(n.message) + "</div>" +
            '<div class="small text-muted">' + escapeHtml(n.createdLocal || "") + "</div>";
        a.addEventListener("click", function (e) {
            e.preventDefault();
            dismissAndOpen(n.id, n.applicationId);
        });
        return a;
    }

    function escapeHtml(text) {
        return String(text || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function upsert(n) {
        if (!n || !n.id) return;
        const id = String(n.id);
        n.id = id;
        if (items.has(id)) return;
        items.set(id, n);
        listEl.prepend(renderItem(n));
        updateBadge();
    }

    function remove(id) {
        id = String(id);
        if (!items.delete(id)) return;
        const node = listEl.querySelector('[data-id="' + id + '"]');
        if (node) node.remove();
        updateBadge();
    }

    function showToast(n) {
        if (!toastHost || !window.bootstrap) return;
        const el = document.createElement("div");
        el.className = "toast align-items-center text-bg-dark border-0";
        el.setAttribute("role", "alert");
        el.innerHTML =
            '<div class="d-flex"><div class="toast-body">' +
            "<strong>" + escapeHtml(n.title) + "</strong><br/>" + escapeHtml(n.message) +
            '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';
        toastHost.appendChild(el);
        const toast = new bootstrap.Toast(el, { delay: 6000 });
        toast.show();
        el.addEventListener("hidden.bs.toast", function () { el.remove(); });
    }

    async function dismissAndOpen(id, applicationId) {
        try {
            await fetch("/Notifications/Dismiss/" + id, {
                method: "POST",
                headers: {
                    "RequestVerificationToken": antiforgery
                }
            });
        } catch (_) { /* still navigate */ }
        window.location.href = detailsUrl(applicationId);
    }

    async function loadActive() {
        try {
            const res = await fetch("/Notifications/Active");
            if (!res.ok) return;
            const data = await res.json();
            (data || []).forEach(upsert);
            updateBadge();
        } catch (_) { /* ignore */ }
    }

    function connectHub() {
        if (!window.signalR) return;
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notifications")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveNotification", function (n) {
            const normalized = {
                id: n.id || n.Id,
                applicationId: n.applicationId || n.ApplicationId,
                title: n.title || n.Title,
                message: n.message || n.Message,
                createdLocal: n.createdLocal || n.CreatedLocal
            };
            upsert(normalized);
            showToast(normalized);
        });

        connection.on("NotificationDismissed", function (id) {
            remove(id);
        });

        connection.start().catch(function () { /* hub unavailable */ });
    }

    loadActive();
    connectHub();
})();
