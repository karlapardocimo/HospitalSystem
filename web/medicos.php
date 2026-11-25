<?php
require_once 'lib/bootstrap.php';
exigir_login();
$r = api()->get('medicos');   // GET /api/medicos (com cache na API)
if (!$r->success) flash_api($r);
require 'views/header.php';
?>
<div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Corpo Clínico (Médicos)</h1>
    <?php if (pode('Administrador')): ?>
    <a href="medico_novo.php" class="btn btn-sm btn-primary"><i class="fas fa-user-md me-1"></i> Cadastrar Novo Médico</a>
    <?php endif; ?>
</div>
<div class="card">
    <div class="card-header bg-white"><i class="fas fa-list me-1 text-primary"></i> <?= e($r->message) ?></div>
    <div class="card-body">
        <div class="table-responsive">
            <table class="table table-hover align-middle">
                <thead><tr><th>ID</th><th>Nome</th><th>CRM</th><th>Especialidade</th><th>Telefone</th><th>E-mail</th><th>Ações</th></tr></thead>
                <tbody>
                <?php foreach ($r->data ?? [] as $m): ?>
                    <tr>
                        <td><?= (int) $m['id'] ?></td>
                        <td><strong><?= e($m['nome']) ?></strong></td>
                        <td><?= e($m['crm']) ?></td>
                        <td><span class="badge bg-primary"><?= e($m['especialidade']) ?></span></td>
                        <td><?= e(formatarTelefone($m['telefone'])) ?></td>
                        <td><?= e($m['email'] ?? '-') ?></td>
                        <td>
                            <?php if (pode('Administrador')): ?>
                            <div class="btn-group">
                                <a href="medico_editar.php?id=<?= (int) $m['id'] ?>" class="btn btn-sm btn-outline-secondary"><i class="fas fa-edit"></i></a>
                                <form method="POST" action="processar_medico.php" onsubmit="return confirm('Excluir este médico?')">
                                    <input type="hidden" name="acao" value="excluir"><input type="hidden" name="id" value="<?= (int) $m['id'] ?>">
                                    <button class="btn btn-sm btn-outline-danger"><i class="fas fa-trash"></i></button>
                                </form>
                            </div>
                            <?php endif; ?>
                        </td>
                    </tr>
                <?php endforeach; ?>
                <?php if (empty($r->data)): ?><tr><td colspan="7" class="text-center">Nenhum médico cadastrado.</td></tr><?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>
<?php require 'views/footer.php'; ?>
