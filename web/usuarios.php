<?php
require_once 'lib/bootstrap.php';
exigir_login();
if (!pode('Administrador')) { flash('warning', 'Somente administradores gerenciam usuários.'); redirecionar('index.php'); }

$r = api()->get('usuarios');   // GET /api/usuarios (somente Administrador)
if (!$r->success) flash_api($r);
$eu = usuario_logado();
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Usuários do Sistema</h1>
    <button class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#modalNovoUsuario"><i class="fas fa-user-plus me-1"></i> Novo Usuário</button>
</div>
<div class="card"><div class="card-body p-0">
    <table class="table table-hover align-middle mb-0">
        <thead class="table-light"><tr><th class="ps-3">Nome</th><th>E-mail</th><th>Perfil</th><th>Situação</th><th>Criado em</th><th class="text-end pe-3">Ações</th></tr></thead>
        <tbody>
        <?php foreach ($r->data ?? [] as $u): $proprio = $u['id'] === $eu['id']; ?>
            <tr>
                <td class="ps-3"><strong><?= e($u['nome']) ?></strong><?= $proprio ? ' <span class="badge bg-light text-dark">você</span>' : '' ?></td>
                <td><?= e($u['email']) ?></td>
                <td colspan="2">
                    <form method="POST" action="processar_usuario.php" class="d-flex gap-1 align-items-center">
                        <input type="hidden" name="acao" value="editar"><input type="hidden" name="id" value="<?= (int) $u['id'] ?>"><input type="hidden" name="nome" value="<?= e($u['nome']) ?>">
                        <select name="perfil" class="form-select form-select-sm w-auto" <?= $proprio ? 'disabled' : '' ?>>
                            <?php foreach ($PERFIS as $perfil): ?><option <?= $perfil === $u['perfil'] ? 'selected' : '' ?>><?= $perfil ?></option><?php endforeach; ?>
                        </select>
                        <select name="ativo" class="form-select form-select-sm w-auto" <?= $proprio ? 'disabled' : '' ?>>
                            <option value="1" <?= $u['ativo'] ? 'selected' : '' ?>>Ativo</option><option value="0" <?= !$u['ativo'] ? 'selected' : '' ?>>Inativo</option>
                        </select>
                        <?php if ($u['bloqueado']): ?><span class="badge bg-danger">Bloqueado</span><?php endif; ?>
                        <?php if (!$proprio): ?><button class="btn btn-sm btn-outline-secondary" title="Salvar"><i class="fas fa-save"></i></button><?php endif; ?>
                    </form>
                </td>
                <td><?= formatarDataHora($u['criadoEm']) ?></td>
                <td class="text-end pe-3">
                    <form method="POST" action="processar_usuario.php" class="d-inline" onsubmit="const s = prompt('Nova senha (mín. 8 caracteres, letras e números):'); if (!s) return false; this.nova_senha.value = s;">
                        <input type="hidden" name="acao" value="senha"><input type="hidden" name="id" value="<?= (int) $u['id'] ?>"><input type="hidden" name="nova_senha">
                        <button class="btn btn-sm btn-outline-primary" title="Redefinir senha"><i class="fas fa-key"></i></button>
                    </form>
                    <?php if (!$proprio): ?>
                    <form method="POST" action="processar_usuario.php" class="d-inline" onsubmit="return confirm('Remover este usuário?')">
                        <input type="hidden" name="acao" value="excluir"><input type="hidden" name="id" value="<?= (int) $u['id'] ?>">
                        <button class="btn btn-sm btn-outline-danger"><i class="fas fa-trash"></i></button>
                    </form>
                    <?php endif; ?>
                </td>
            </tr>
        <?php endforeach; ?>
        </tbody>
    </table>
</div></div>

<div class="modal fade" id="modalNovoUsuario" tabindex="-1"><div class="modal-dialog"><div class="modal-content">
    <form method="POST" action="processar_usuario.php">
        <input type="hidden" name="acao" value="novo">
        <div class="modal-header"><h5 class="modal-title">Cadastrar Usuário</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
        <div class="modal-body">
            <div class="mb-3"><label class="form-label">Nome</label><input class="form-control" name="nome" required></div>
            <div class="mb-3"><label class="form-label">E-mail</label><input type="email" class="form-control" name="email" required></div>
            <div class="mb-3"><label class="form-label">Senha</label><input type="password" class="form-control" name="senha" minlength="8" required>
                <small class="text-muted">Mínimo 8 caracteres, com letras e números. A API grava apenas o hash (PBKDF2).</small></div>
            <div class="mb-3"><label class="form-label">Perfil</label>
                <select class="form-select" name="perfil"><?php foreach ($PERFIS as $perfil): ?><option><?= $perfil ?></option><?php endforeach; ?></select>
            </div>
        </div>
        <div class="modal-footer"><button class="btn btn-primary">Cadastrar</button></div>
    </form>
</div></div></div>
<?php require 'views/footer.php'; ?>
