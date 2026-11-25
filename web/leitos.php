<?php
require_once 'lib/bootstrap.php';
exigir_login();

$filtro = $_GET['filtro'] ?? 'Todos';
$r = api()->get('leitos', ['status' => $filtro]);   // GET /api/leitos?status= (cache de 1 min na API)
if (!$r->success) flash_api($r);
$podeOperar = pode('Administrador', 'Medico', 'Enfermagem');

// Ações manuais permitidas por status (RN-LEI-04). A API valida de novo.
$acoes = [
    'Disponível' => ['Higienização' => 'Enviar p/ higienização', 'Manutenção' => 'Colocar em manutenção'],
    'Higienização' => ['Disponível' => 'Liberar (higienizado)', 'Manutenção' => 'Colocar em manutenção'],
    'Manutenção' => ['Disponível' => 'Liberar (manutenção concluída)'],
    'Ocupado' => [],
];
require 'views/header.php';
?>
<div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Mapa de Leitos Hospitalares</h1>
    <div class="btn-toolbar mb-2 mb-md-0">
        <div class="btn-group me-2">
            <?php foreach (['Todos', 'Disponível', 'Ocupado', 'Higienização', 'Manutenção'] as $f): ?>
                <a href="leitos.php?filtro=<?= urlencode($f) ?>" class="btn btn-sm <?= $filtro === $f ? 'btn-secondary' : 'btn-outline-secondary' ?>"><?= $f ?></a>
            <?php endforeach; ?>
        </div>
        <?php if (pode('Administrador')): ?>
        <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#modalNovoLeito"><i class="fas fa-plus me-1"></i> Novo Leito</button>
        <?php endif; ?>
    </div>
</div>

<div class="row row-cols-1 row-cols-md-2 row-cols-lg-4 g-4">
<?php foreach ($r->data ?? [] as $l): $cor = classeStatusLeito($l['status']); ?>
    <div class="col">
        <div class="card h-100 border-top border-4 border-<?= $cor ?>">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-start mb-2">
                    <h5 class="card-title mb-0">Leito <?= e($l['numero']) ?></h5>
                    <span class="badge bg-<?= $cor ?><?= $cor === 'warning' ? ' text-dark' : '' ?>"><?= e($l['status']) ?></span>
                </div>
                <p class="card-text text-muted small mb-1"><i class="fas fa-layer-group me-1"></i> <?= e($l['ala']) ?></p>
                <?php if ($l['pacienteAtual']): ?>
                    <p class="small mb-0"><i class="fas fa-user-injured me-1"></i> <?= e($l['pacienteAtual']) ?></p>
                <?php endif; ?>
            </div>
            <div class="card-footer bg-transparent border-0 d-flex justify-content-between">
                <?php if (pode('Administrador')): ?>
                    <a href="leito_editar.php?id=<?= (int) $l['id'] ?>" class="btn btn-sm btn-outline-primary"><i class="fas fa-edit"></i></a>
                <?php else: ?><span></span><?php endif; ?>
                <?php if ($podeOperar): ?>
                <div class="dropdown">
                    <button class="btn btn-sm btn-light dropdown-toggle" type="button" data-bs-toggle="dropdown">Ações</button>
                    <ul class="dropdown-menu dropdown-menu-end">
                        <?php if ($l['status'] === 'Disponível'): ?>
                            <li><a class="dropdown-item" href="internacoes.php?leito=<?= (int) $l['id'] ?>"><i class="fas fa-procedures me-1"></i> Internar paciente</a></li>
                        <?php elseif ($l['status'] === 'Ocupado'): ?>
                            <li><a class="dropdown-item" href="internacoes.php"><i class="fas fa-procedures me-1"></i> Ver internação (alta/transferência)</a></li>
                        <?php endif; ?>
                        <?php foreach ($acoes[$l['status']] ?? [] as $novo => $rotulo): ?>
                            <li>
                                <form method="POST" action="processar_leitos.php">
                                    <input type="hidden" name="acao" value="status"><input type="hidden" name="id" value="<?= (int) $l['id'] ?>">
                                    <input type="hidden" name="status" value="<?= e($novo) ?>"><input type="hidden" name="filtro" value="<?= e($filtro) ?>">
                                    <button class="dropdown-item"><?= e($rotulo) ?></button>
                                </form>
                            </li>
                        <?php endforeach; ?>
                    </ul>
                </div>
                <?php endif; ?>
            </div>
        </div>
    </div>
<?php endforeach; ?>
<?php if (empty($r->data)): ?><div class="col-12 text-center py-5"><p class="text-muted">Nenhum leito encontrado.</p></div><?php endif; ?>
</div>

<div class="modal fade" id="modalNovoLeito" tabindex="-1">
    <div class="modal-dialog"><div class="modal-content">
        <form method="POST" action="processar_leitos.php">
            <input type="hidden" name="acao" value="novo">
            <div class="modal-header"><h5 class="modal-title">Novo Leito</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
            <div class="modal-body">
                <div class="mb-3"><label class="form-label">Número</label><input class="form-control" name="numero" maxlength="10" required></div>
                <div class="mb-3"><label class="form-label">Ala / Setor</label><input class="form-control" name="ala" required></div>
            </div>
            <div class="modal-footer"><button class="btn btn-primary">Salvar</button></div>
        </form>
    </div></div>
</div>
<?php require 'views/footer.php'; ?>
