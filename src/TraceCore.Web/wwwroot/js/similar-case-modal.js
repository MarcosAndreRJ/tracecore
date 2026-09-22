// Modal de resumo rápido para "Casos Semelhantes" — usado tanto na Abertura de Caso
// (Cases/Create) quanto nos Detalhes do Caso (Cases/Details). Espelha as classes/ícones/
// rótulos de _CaseStatusBadge.cshtml e _SeverityBadge.cshtml (mesma lógica de cores
// calculada no servidor para os badges normais da tela).
(function () {
    function buildStatusBadge(status) {
        status = status || 'Open';
        var isResolved = status.toLowerCase() === 'resolved';
        var isOpen = status.toLowerCase() === 'open';
        var cls = isResolved ? 'tc-badge-confirmed' : (isOpen ? 'tc-badge-open' : 'tc-badge-closed');
        var icon = isResolved ? 'bi-check2-all' : (isOpen ? 'bi-record-circle' : 'bi-check-circle');
        var label = isResolved ? 'Resolvido' : (isOpen ? 'Aberto' : 'Fechado');
        return '<span class="tc-badge ' + cls + '"><i class="bi ' + icon + '"></i><span>' + label + '</span></span>';
    }

    function buildSeverityBadge(severity) {
        var sev = (severity || 'Low').toLowerCase();
        var map = {
            critical: ['tc-badge-critical', 'bi-exclamation-octagon-fill', 'Crítica'],
            high: ['tc-badge-high', 'bi-exclamation-triangle-fill', 'Alta'],
            medium: ['tc-badge-medium', 'bi-dash-circle-fill', 'Média']
        };
        var entry = map[sev] || ['tc-badge-low', 'bi-info-circle-fill', 'Baixa'];
        return '<span class="tc-badge ' + entry[0] + '"><i class="bi ' + entry[1] + '"></i><span>' + entry[2] + '</span></span>';
    }

    function init() {
        var modalEl = document.getElementById('similarCaseModal');
        if (!modalEl) return;

        modalEl.addEventListener('show.bs.modal', function (ev) {
            var trigger = ev.relatedTarget;
            if (!trigger) return;

            document.getElementById('simModalCaseNumber').textContent = '#' + trigger.dataset.caseNumber;
            document.getElementById('simModalTitle').textContent = trigger.dataset.title || '';
            document.getElementById('simModalLikelihood').textContent =
                (trigger.dataset.likelihood || '') + ' — score ' + trigger.dataset.score;
            document.getElementById('simModalDetailsLink').href = trigger.dataset.detailsUrl || '#';
            document.getElementById('simModalStatusBadge').innerHTML = buildStatusBadge(trigger.dataset.status);
            document.getElementById('simModalSeverityBadge').innerHTML = buildSeverityBadge(trigger.dataset.severity);

            var factorsWrap = document.getElementById('simModalFactors');
            factorsWrap.innerHTML = '';
            (trigger.dataset.factors || '').split('|').filter(function (f) { return f; }).forEach(function (f) {
                var span = document.createElement('span');
                span.className = 'badge bg-info-subtle text-info border border-info-subtle small';
                var icon = document.createElement('i');
                icon.className = 'bi bi-check2 me-1';
                span.appendChild(icon);
                span.appendChild(document.createTextNode(f));
                factorsWrap.appendChild(span);
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
