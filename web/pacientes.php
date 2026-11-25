<?php
require_once 'lib/bootstrap.php';
exigir_login();

$busca = trim($_GET['busca'] ?? '');
$r = api()->get('pacientes', ['busca' => $busca]);   // GET /api/pacientes?busca=
$pacientes = $r->success ? $r->data : [];
if (!$r->success) flash_api($r);

require 'views/header.php';
?>
<div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Gestão de Pacientes</h1>
    <?php if (pode('Administrador', 'Recepcao', 'Enfermagem')): ?>
    <a href="paciente_novo.php" class="btn btn-primary"><i class="fas fa-plus me-1"></i> Novo Paciente</a>
    <?php endif; ?>
</div>

<div class="card">
    <div class="card-header d-flex justify-content-between align-items-center">
        <span><?= e($r->message) ?></span>
        <form class="input-group input-group-sm w-auto" method="GET">
            <input type="text" class="form-control" name="busca" value="<?= e($busca) ?>" placeholder="Buscar por nome ou CPF...">
            <button class="btn btn-outline-secondary" type="submit"><i class="fas fa-search"></i></button>
            <?php if ($busca !== ''): ?><a class="btn btn-outline-secondary" href="pacientes.php">Limpar</a><?php endif; ?>
        </form>
    </div>
    <div class="card-body">
        <div class="table-responsive">
            <table class="table table-striped table-hover align-middle">
                <thead>
                    <tr><th>ID</th><th>Nome</th><th>CPF</th><th>Nascimento</th><th>Idade</th><th>Telefone</th><th>Tipo Sanguíneo</th><th>Ações</th></tr>
                </thead>
                <tbody>
                <?php foreach ($pacientes as $p): ?>
                    <tr>
                        <td><?= (int) $p['id'] ?></td>
                        <td><strong><?= e($p['nome']) ?></strong><?php if ($p['alergias']): ?> <span class="badge bg-danger-subtle text-danger" title="Alergias: <?= e($p['alergias']) ?>"><i class="fas fa-allergies"></i></span><?php endif; ?></td>
                        <td><?= e($p['cpfFormatado']) ?></td>
                        <td><?= formatarData($p['dataNascimento']) ?></td>
                        <td><?= (int) $p['idade'] ?></td>
                        <td><?= e(formatarTelefone($p['telefone'])) ?></td>
                        <td><span class="badge bg-info text-dark"><?= e($p['tipoSanguineo'] ?? '-') ?></span></td>
                        <td>
                            <div class="btn-group">
                                <a href="prontuario.php?id=<?= (int) $p['id'] ?>" class="btn btn-sm btn-outline-primary" title="Ver Prontuário"><i class="fas fa-file-medical"></i></a>
                                <?php if (pode('Administrador', 'Recepcao', 'Enfermagem')): ?>
                                <a href="paciente_editar.php?id=<?= (int) $p['id'] ?>" class="btn btn-sm btn-outline-secondary" title="Editar"><i class="fas fa-edit"></i></a>
                                <?php endif; ?>
                                <?php if (pode('Administrador')): ?>
                                <form method="POST" action="processar_paciente.php" class="d-inline" onsubmit="return confirm('Tem certeza que deseja excluir este paciente?')">
                                    <input type="hidden" name="acao" value="excluir"><input type="hidden" name="id" value="<?= (int) $p['id'] ?>">
                                    <button class="btn btn-sm btn-outline-danger" title="Excluir"><i class="fas fa-trash"></i></button>
                                </form>
                                <?php endif; ?>
                            </div>
                        </td>
                    </tr>
                <?php endforeach; ?>
                <?php if (!$pacientes): ?>
                    <tr><td colspan="8" class="text-center text-muted py-4">Nenhum paciente encontrado.</td></tr>
                <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>
<?php require 'views/footer.php'; ?>
