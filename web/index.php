<?php
require_once 'lib/bootstrap.php';
exigir_login();

// Indicadores vêm da API (GET /api/dashboard, com cache de 30 s no servidor)
$r = api()->get('dashboard');
$d = $r->success ? $r->data : ['pacientesInternados' => 0, 'leitosDisponiveis' => 0, 'consultasHoje' => 0,
    'triagensPendentes' => 0, 'alertasTriagem' => [], 'proximasConsultas' => [], 'geradoEm' => null];
if (!$r->success) flash_api($r);

require 'views/header.php';
?>
<div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Dashboard Administrativo</h1>
    <div class="btn-toolbar mb-2 mb-md-0">
        <small class="text-muted me-3 align-self-center">Atualizado em <?= formatarDataHora($d['geradoEm']) ?></small>
        <button type="button" class="btn btn-sm btn-outline-secondary" onclick="window.print()">
            <i class="fas fa-print me-1"></i> Relatório Diário
        </button>
    </div>
</div>

<div class="row g-3 mb-4">
    <?php foreach ([
        ['Pacientes Internados', $d['pacientesInternados'], 'bg-primary text-white', 'fa-user-injured'],
        ['Leitos Disponíveis', $d['leitosDisponiveis'], 'bg-success text-white', 'fa-bed'],
        ['Consultas Hoje', $d['consultasHoje'], 'bg-warning text-dark', 'fa-calendar-check'],
        ['Triagens Pendentes', $d['triagensPendentes'], 'bg-danger text-white', 'fa-notes-medical'],
    ] as [$titulo, $valor, $classe, $icone]): ?>
    <div class="col-12 col-sm-6 col-xl-3">
        <div class="card border-0 shadow-sm <?= $classe ?> h-100">
            <div class="card-body d-flex align-items-center justify-content-between">
                <div>
                    <h6 class="text-uppercase mb-1" style="font-size: 0.75rem; opacity: 0.8;"><?= $titulo ?></h6>
                    <h2 class="mb-0"><?= (int) $valor ?></h2>
                </div>
                <div class="fs-1 opacity-25"><i class="fas <?= $icone ?>"></i></div>
            </div>
        </div>
    </div>
    <?php endforeach; ?>
</div>

<div class="row g-4">
    <div class="col-12 col-lg-8">
        <div class="card border-0 shadow-sm h-100">
            <div class="card-header bg-white py-3 border-0">
                <h6 class="m-0 text-primary"><i class="fas fa-clock me-2"></i>Próximas Consultas de Hoje</h6>
            </div>
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-hover align-middle mb-0">
                        <thead class="table-light">
                            <tr><th>Horário</th><th>Paciente</th><th>Médico</th><th>Status</th><th class="text-end">Ações</th></tr>
                        </thead>
                        <tbody>
                        <?php if (!empty($d['proximasConsultas'])): ?>
                            <?php foreach ($d['proximasConsultas'] as $c): ?>
                            <tr>
                                <td><?= formatarHora($c['horario']) ?></td>
                                <td><strong><?= e($c['pacienteNome']) ?></strong></td>
                                <td><?= e($c['medicoNome'] ?? '-') ?> <small class="text-muted"><?= $c['especialidade'] ? '(' . e($c['especialidade']) . ')' : '' ?></small></td>
                                <td><span class="badge rounded-pill bg-warning text-dark"><?= e($c['status']) ?></span></td>
                                <td class="text-end"><a href="prontuario.php?id=<?= (int) $c['idPaciente'] ?>" class="btn btn-sm btn-outline-primary">Ver</a></td>
                            </tr>
                            <?php endforeach; ?>
                        <?php else: ?>
                            <tr><td colspan="5" class="text-center py-5 text-muted">Nenhuma consulta pendente para hoje.</td></tr>
                        <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <div class="col-12 col-lg-4">
        <div class="card border-0 shadow-sm h-100">
            <div class="card-header bg-white py-3 border-0">
                <h6 class="m-0 text-danger"><i class="fas fa-exclamation-triangle me-2"></i>Alertas de Triagem</h6>
            </div>
            <div class="card-body">
                <?php if (!empty($d['alertasTriagem'])): ?>
                    <div class="list-group list-group-flush">
                    <?php foreach ($d['alertasTriagem'] as $a): ?>
                        <div class="list-group-item px-0 border-0 mb-3">
                            <div class="d-flex w-100 justify-content-between align-items-center mb-1">
                                <span class="badge <?= classeRisco($a['classificacaoRisco']) ?>"><?= e($a['classificacaoRisco']) ?></span>
                                <small class="text-muted">Em atendimento</small>
                            </div>
                            <p class="mb-1 text-dark">Paciente: <strong><?= e($a['pacienteNome']) ?></strong></p>
                            <small class="text-muted">PA: <?= e($a['pressaoArterial']) ?> | T: <?= e($a['temperatura']) ?>°C</small>
                        </div>
                    <?php endforeach; ?>
                    </div>
                    <div class="d-grid mt-2"><a href="consultas.php" class="btn btn-primary btn-sm">Ver fila de atendimento</a></div>
                <?php else: ?>
                    <div class="text-center py-5">
                        <i class="fas fa-check-circle text-success fs-1 mb-3"></i>
                        <p class="text-muted">Nenhum paciente crítico em espera.</p>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>
</div>
<?php require 'views/footer.php'; ?>
