<?php // Espera $m (dados atuais ou []) ?>
<div class="row g-3">
    <div class="col-md-8"><label class="form-label">Nome Completo</label><input type="text" class="form-control" name="nome" value="<?= e(old('nome', $m['nome'] ?? '')) ?>" required></div>
    <div class="col-md-4"><label class="form-label">CRM</label><input type="text" class="form-control" name="crm" value="<?= e(old('crm', $m['crm'] ?? '')) ?>" placeholder="Somente números" required></div>
    <div class="col-md-6">
        <label class="form-label">Especialidade</label>
        <select class="form-select" name="especialidade" required>
            <option value="">Selecione...</option>
            <?php foreach ($GLOBALS['ESPECIALIDADES'] as $esp): ?>
                <option value="<?= e($esp) ?>" <?= old('especialidade', $m['especialidade'] ?? '') === $esp ? 'selected' : '' ?>><?= e($esp) ?></option>
            <?php endforeach; ?>
        </select>
    </div>
    <div class="col-md-6"><label class="form-label">Telefone</label><input type="text" class="form-control" name="telefone" value="<?= e(old('telefone', $m['telefone'] ?? '')) ?>"></div>
    <div class="col-12"><label class="form-label">E-mail</label><input type="email" class="form-control" name="email" value="<?= e(old('email', $m['email'] ?? '')) ?>"></div>
</div>
