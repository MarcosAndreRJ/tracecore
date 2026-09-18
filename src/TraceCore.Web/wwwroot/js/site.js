// TraceCore Web - Interações de Interface e Acessibilidade (WCAG 2.2 AA)

document.addEventListener("DOMContentLoaded", function () {
    const body = document.body;
    const sidebarToggleBtn = document.getElementById("tc-sidebar-toggle");
    const mobileSidebarToggleBtn = document.getElementById("tc-mobile-sidebar-toggle");
    const backdrop = document.getElementById("tc-sidebar-backdrop");
    const searchInput = document.getElementById("tc-global-search");

    // 1. Restaurar estado da Sidebar via localStorage
    const isSidebarCollapsed = localStorage.getItem("tracecore_sidebar_collapsed") === "true";
    if (isSidebarCollapsed && window.innerWidth >= 992) {
        body.classList.add("sidebar-collapsed");
    }

    // 2. Alternância de Recolhimento da Sidebar (Desktop)
    if (sidebarToggleBtn) {
        sidebarToggleBtn.addEventListener("click", function () {
            body.classList.toggle("sidebar-collapsed");
            const collapsed = body.classList.contains("sidebar-collapsed");
            localStorage.setItem("tracecore_sidebar_collapsed", collapsed ? "true" : "false");
        });
    }

    // 3. Alternância da Sidebar em Telas Mobile
    if (mobileSidebarToggleBtn) {
        mobileSidebarToggleBtn.addEventListener("click", function () {
            body.classList.toggle("sidebar-mobile-open");
        });
    }

    if (backdrop) {
        backdrop.addEventListener("click", function () {
            body.classList.remove("sidebar-mobile-open");
        });
    }

    // 4. Atalho de Teclado Global: Ctrl+K / Cmd+K para busca
    document.addEventListener("keydown", function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
            if (searchInput) {
                e.preventDefault();
                searchInput.focus();
                searchInput.select();
            }
        }
    });

    // 5. Submit da busca global -> submete o formulário GET para /Search
    if (searchInput) {
        searchInput.addEventListener("keypress", function (e) {
            if (e.key === "Enter") {
                const form = searchInput.closest("form");
                if (form) {
                    form.submit();
                }
            }
        });
    }
});
