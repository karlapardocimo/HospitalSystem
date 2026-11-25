<?php
require_once 'lib/bootstrap.php';
exigir_login();
$r = api()->get('convenios');   // GET /api/convenios (com cache na API)
if (!$r->success) flash_api($r);
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Gestão de Convênios</h1>
    <?php if (pode('Administrador')): ?>
    <button class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#modalNovoConvenio"><i class="fas fa-plus me-1"></i> Novo Convênio</button>
    <?php endif; ?>
</div>
<div class="card shadow-sm border-0"><div class="card-body p-0">
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
            <thead class="table-light"><tr><th class="ps-4">ID</th><th>Nome do Convênio</th><th>Validade</th><th>Telefone</th><th class="text-end pe-4">Ações</th></tr></thead>
            <tbody>
            <?php foreach ($r->data ?? [] as $c): ?>
                <tr>
                    <td class="ps-4">#<?= (int) $c['id'] ?></td>
                    <td><strong><?= e($c['nome']) ?></strong></td>
                    <td><?= formatarData($c['validadeCarteira']) ?> <?php if ($c['vencido']): ?><span class="badge bg-danger">Vencido</span><?php endif; ?></td>
                    <td><?= $c['telefone'] ? e(formatarTelefone($c['telefone'])) : '<span class="text-muted">Não informado</span>' ?></td>
                    <td class="text-end pe-4">
                        <?php if (pode('Administrador')): ?>
                        <div class="btn-group">
                            <a href="convenio_editar.php?id=<?= (int) $c['id'] ?>" class="btn btn-sm btn-outline-primary"><i class="fas fa-edit"></i></a>
                            <form method="POST" action="processar_administrativo.php" onsubmit="return confirm('Remover este convênio?')">
                                <input type="hidden" name="acao" value="excluir_convenio"><input type="hidden" name="id" value="<?= (int) $c['id'] ?>">
                                <button class="btn btn-sm btn-outline-danger"><i class="fas fa-trash"></i></button>
                            </form>
                        </div>
                        <?php endif; ?>
                    </td>
                </tr>
            <?php endforeach; ?>
            <?php if (empty($r->data)): ?><tr><td colspan="5" class="text-center py-5 text-muted">Nenhum convênio cadastrado.</td></tr><?php endif; ?>
            </tbody>
        </table>
    </div>
</div></div>

<div class="modal fade" id="modalNovoConvenio" tabindex="-1">
    <div class="modal-dialog"><div class="modal-content">
        <form action="processar_administrativo.php" method="POST">
            <input type="hidden" name="acao" value="novo_convenio">
            <div class="modal-header"><h5 class="modal-title">Cadastrar Novo Convênio</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
            <div class="modal-body">
                <div class="mb-3"><label class="form-label">Nome do Convênio</label><input type="text" class="form-control" name="nome" placeholder="Ex: Unimed, Bradesco..." required></div>
                <div class="mb-3"><label class="form-label">Validade</label><input type="date" class="form-control" name="validade"></div>
                <div class="mb-3"><label class="form-label">Telefone de Contato</label><input type="text" class="form-control" name="telefone" placeholder="(00) 0000-0000"></div>
            </div>
            <div class="modal-footer"><button type="submit" class="btn btn-primary">Salvar Convênio</button></div>
        </form>
    </div></div>
</div>
<?php require 'views/footer.php'; ?>
