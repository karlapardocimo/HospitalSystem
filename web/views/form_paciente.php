<?php
// Formulário compartilhado entre cadastro e edição. Espera $p (array com os dados atuais) e $edicao (bool).
?>
<div class="row g-3">
    <div class="col-md-6">
        <label for="nome" class="form-label">Nome Completo</label>
        <input type="text" class="form-control" id="nome" name="nome" required value="<?= e(old('nome', $p['nome'] ?? '')) ?>" placeholder="Ex: João da Silva">
    </div>
    <div class="col-md-3">
        <label for="cpf" class="form-label">CPF</label>
        <input type="text" class="form-control" id="cpf" name="cpf" required value="<?= e(old('cpf', isset($p['cpf']) ? formatarCPF($p['cpf']) : '')) ?>" placeholder="000.000.000-00">
    </div>
    <div class="col-md-3">
        <label for="data_nascimento" class="form-label">Data de Nascimento</label>
        <input type="date" class="form-control" id="data_nascimento" name="data_nascimento" required value="<?= e(old('data_nascimento', $p['dataNascimento'] ?? '')) ?>">
    </div>
    <div class="col-md-4">
        <label for="telefone" class="form-label">Telefone de Contato</label>
        <input type="text" class="form-control" id="telefone" name="telefone" value="<?= e(old('telefone', isset($p['telefone']) ? formatarTelefone($p['telefone']) : '')) ?>" placeholder="(00) 00000-0000">
    </div>
    <div class="col-md-4">
        <label for="tipo_sanguineo" class="form-label">Tipo Sanguíneo</label>
        <select class="form-select" id="tipo_sanguineo" name="tipo_sanguineo">
            <option value="">Não informado</option>
            <?php foreach ($GLOBALS['TIPOS_SANGUINEOS'] as $tipo): ?>
                <option value="<?= e($tipo) ?>" <?= old('tipo_sanguineo', $p['tipoSanguineo'] ?? '') === $tipo ? 'selected' : '' ?>><?= e($tipo) ?></option>
            <?php endforeach; ?>
        </select>
    </div>
    <?php if (!$edicao): ?>
    <div class="col-md-4 d-flex align-items-end">
        <div class="form-check">
            <input class="form-check-input" type="checkbox" name="enviar_triagem" id="enviar_triagem" value="1" <?= old('enviar_triagem', '1') ? 'checked' : '' ?>>
            <label class="form-check-label" for="enviar_triagem">Enviar para a fila de triagem</label>
        </div>
    </div>
    <?php endif; ?>
    <div class="col-12">
        <label for="endereco" class="form-label">Endereço Completo</label>
        <input type="text" class="form-control" id="endereco" name="endereco" value="<?= e(old('endereco', $p['endereco'] ?? '')) ?>" placeholder="Rua, Número, Bairro, Cidade - UF">
    </div>
    <div class="col-md-6">
        <label for="alergias" class="form-label">Alergias Conhecidas</label>
        <textarea class="form-control" id="alergias" name="alergias" rows="3" placeholder="Medicamentos ou substâncias (são verificados em toda prescrição)"><?= e(old('alergias', $p['alergias'] ?? '')) ?></textarea>
    </div>
    <div class="col-md-6">
        <label for="historico" class="form-label">Histórico Clínico Relevante</label>
        <textarea class="form-control" id="historico" name="historico_clinico" rows="3" placeholder="Doenças crônicas, cirurgias prévias, etc..."><?= e(old('historico_clinico', $p['historicoClinico'] ?? '')) ?></textarea>
    </div>
</div>
