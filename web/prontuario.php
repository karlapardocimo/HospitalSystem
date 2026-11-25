<?php
require_once 'lib/bootstrap.php';
exigir_login();

$id = (int) ($_GET['id'] ?? 0);
$r = api()->get("pacientes/$id/prontuario");     // GET /api/pacientes/{id}/prontuario
if (!$r->success) {
    flash_api($r);
    redirecionar('pacientes.php');
}
$pac = $r->data['paciente'];
$emAtendimento = $r->data['consultaEmAtendimento'];
$triagens = $r->data['triagens'];
$registros = $r->data['registros'];
$medicamentos = pode('Medico') ? (api()->get('medicamentos')->data ?? []) : [];
$pendente = $_SESSION['prescricao_pendente'] ?? null;   // prescrição bloqueada por interação (RN-PRE-04)
unset($_SESSION['prescricao_pendente']);
$voltar = "prontuario.php?id=$id";

require 'views/header.php';
?>
<div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Prontuário Eletrônico: <?= e($pac['nome']) ?></h1>
    <div class="btn-toolbar mb-2 mb-md-0 gap-2">
        <button class="btn btn-sm btn-outline-primary" onclick="window.print()"><i class="fas fa-print me-1"></i> Imprimir</button>
        <?php if ($emAtendimento && pode('Administrador', 'Medico', 'Enfermagem')): ?>
        <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#modalEvolucao"><i class="fas fa-plus me-1"></i> Novo Registro Clínico</button>
        <?php endif; ?>
        <?php if ($emAtendimento && pode('Administrador', 'Medico')): ?>
        <form method="POST" action="processar_consultas.php" onsubmit="return confirm('Finalizar o atendimento e liberar o paciente?')">
            <input type="hidden" name="acao" value="liberar"><input type="hidden" name="id" value="<?= (int) $emAtendimento[0]['id'] ?>">
            <input type="hidden" name="voltar" value="<?= e($voltar) ?>">
            <button class="btn btn-sm btn-success"><i class="fas fa-door-open me-1"></i> Liberar Paciente</button>
        </form>
        <?php endif; ?>
    </div>
</div>

<?php if ($pendente): ?>
<div class="alert alert-warning">
    <h6 class="alert-heading"><i class="fas fa-exclamation-triangle me-1"></i> Interação medicamentosa detectada</h6>
    <ul class="mb-2"><?php foreach ($pendente['alertas'] as $a): ?><li><?= e($a) ?></li><?php endforeach; ?></ul>
    <form method="POST" action="processar_prontuario.php" class="d-inline">
        <?php foreach (['id_prontuario', 'id_medicamento', 'dosagem', 'frequencia', 'duracao'] as $campo): ?>
            <input type="hidden" name="<?= $campo ?>" value="<?= e($pendente[$campo]) ?>">
        <?php endforeach; ?>
        <input type="hidden" name="acao" value="prescrever"><input type="hidden" name="confirmar_interacao" value="1">
        <input type="hidden" name="id_paciente" value="<?= $id ?>">
        <button class="btn btn-sm btn-warning">Estou ciente — prescrever mesmo assim</button>
    </form>
    <a href="<?= e($voltar) ?>" class="btn btn-sm btn-outline-secondary">Descartar</a>
</div>
<?php endif; ?>

<div class="row">
    <div class="col-md-4">
        <div class="card">
            <div class="card-header">Informações do Paciente</div>
            <div class="card-body">
                <p><strong>CPF:</strong> <?= e($pac['cpfFormatado']) ?></p>
                <p><strong>Nascimento:</strong> <?= formatarData($pac['dataNascimento']) ?> (<?= (int) $pac['idade'] ?> anos)</p>
                <p><strong>Tipo Sanguíneo:</strong> <span class="badge bg-danger"><?= e($pac['tipoSanguineo'] ?? '-') ?></span></p>
                <p><strong>Telefone:</strong> <?= e(formatarTelefone($pac['telefone'])) ?></p>
                <hr>
                <h6>Alergias:</h6>
                <p class="text-danger"><?= e($pac['alergias'] ?: 'Nenhuma registrada.') ?></p>
                <h6>Histórico clínico:</h6>
                <p class="text-muted"><?= e($pac['historicoClinico'] ?: '-') ?></p>
                <?php if ($emAtendimento): ?>
                    <div class="alert alert-info small mb-0">Em atendimento — consulta #<?= (int) $emAtendimento[0]['id'] ?><?= $emAtendimento[0]['medicoNome'] ? ' com ' . e($emAtendimento[0]['medicoNome']) : '' ?></div>
                <?php endif; ?>
            </div>
        </div>
        <div class="card">
            <div class="card-header">Triagens</div>
            <div class="card-body small">
                <?php foreach ($triagens as $t): ?>
                    <div class="mb-2 pb-2 border-bottom">
                        <span class="badge <?= classeRisco($t['classificacaoRisco']) ?>"><?= e($t['classificacaoRisco']) ?></span>
                        <span class="text-muted"><?= formatarDataHora($t['dataTriagem']) ?></span><br>
                        PA <?= e($t['pressaoArterial']) ?> · T <?= e($t['temperatura']) ?>°C · FC <?= (int) $t['frequenciaCardiaca'] ?> · Sat <?= (int) $t['saturacaoOxigenio'] ?>% · Dor <?= (int) $t['escalaDor'] ?>
                        <?php if ($t['queixaPrincipal']): ?><br><em><?= e($t['queixaPrincipal']) ?></em><?php endif; ?>
                    </div>
                <?php endforeach; ?>
                <?php if (!$triagens): ?><span class="text-muted">Nenhuma triagem registrada.</span><?php endif; ?>
            </div>
        </div>
    </div>

    <div class="col-md-8">
        <div class="card">
            <div class="card-header">Histórico de Evolução Clínica</div>
            <div class="card-body">
                <?php foreach ($registros as $reg): ?>
                <div class="border-start border-3 border-primary ps-4 mb-4">
                    <span class="text-muted small"><?= formatarDataHora($reg['dataRegistro']) ?> - <?= e($reg['profissional'] ?? '') ?> · Registro #<?= (int) $reg['id'] ?></span>
                    <p class="mb-1"><?= nl2br(e($reg['evolucaoClinica'])) ?></p>
                    <?php if ($reg['observacoes']): ?><p class="small text-muted mb-1">Obs.: <?= e($reg['observacoes']) ?></p><?php endif; ?>

                    <?php if ($reg['prescricoes']): ?>
                        <h6 class="small mt-2 mb-1"><i class="fas fa-prescription me-1"></i> Prescrições</h6>
                        <ul class="small mb-1">
                        <?php foreach ($reg['prescricoes'] as $pr): ?>
                            <li><strong><?= e($pr['nomeMedicamento']) ?></strong> <?= e($pr['dosagem']) ?> · <?= e($pr['frequencia']) ?> · <?= e($pr['duracaoTratamento']) ?>
                                <?php if ($pr['alertaConfirmado']): ?><span class="badge bg-warning text-dark" title="<?= e($pr['alertaConfirmado']) ?>">interação confirmada</span><?php endif; ?></li>
                        <?php endforeach; ?>
                        </ul>
                    <?php endif; ?>

                    <?php if ($reg['exames']): ?>
                        <h6 class="small mt-2 mb-1"><i class="fas fa-vial me-1"></i> Exames</h6>
                        <ul class="small mb-1">
                        <?php foreach ($reg['exames'] as $ex): ?>
                            <li><strong><?= e($ex['nomeExame']) ?></strong> (<?= formatarData($ex['dataExame']) ?>):
                                <?php if ($ex['resultado']): ?><?= e($ex['resultado']) ?>
                                <?php elseif (pode('Administrador', 'Medico')): ?>
                                    <form method="POST" action="processar_prontuario.php" class="d-inline-flex gap-1">
                                        <input type="hidden" name="acao" value="resultado"><input type="hidden" name="id_exame" value="<?= (int) $ex['id'] ?>"><input type="hidden" name="id_paciente" value="<?= $id ?>">
                                        <input class="form-control form-control-sm" name="resultado" placeholder="Resultado" required>
                                        <button class="btn btn-sm btn-outline-primary">Salvar</button>
                                    </form>
                                <?php else: ?><em class="text-muted">aguardando resultado</em><?php endif; ?>
                            </li>
                        <?php endforeach; ?>
                        </ul>
                    <?php endif; ?>

                    <div class="mt-2">
                        <?php if (pode('Medico')): ?>
                        <button class="btn btn-sm btn-outline-success" onclick="abrirPrescricao(<?= (int) $reg['id'] ?>)"><i class="fas fa-prescription me-1"></i> Prescrever</button>
                        <?php endif; ?>
                        <?php if (pode('Administrador', 'Medico')): ?>
                        <button class="btn btn-sm btn-outline-secondary" onclick="abrirExame(<?= (int) $reg['id'] ?>)"><i class="fas fa-vial me-1"></i> Solicitar exame</button>
                        <?php endif; ?>
                    </div>
                </div>
                <?php endforeach; ?>
                <?php if (!$registros): ?>
                    <p class="text-muted mb-0">Nenhum registro clínico. <?= $emAtendimento ? 'Use "Novo Registro Clínico" para iniciar.' : 'O registro é liberado quando o paciente está em atendimento (após a triagem).' ?></p>
                <?php endif; ?>
            </div>
        </div>
    </div>
</div>

<?php if ($emAtendimento): ?>
<div class="modal fade" id="modalEvolucao" tabindex="-1">
    <div class="modal-dialog modal-lg"><div class="modal-content">
        <form method="POST" action="processar_prontuario.php">
            <input type="hidden" name="acao" value="evolucao"><input type="hidden" name="id_paciente" value="<?= $id ?>">
            <input type="hidden" name="id_consulta" value="<?= (int) $emAtendimento[0]['id'] ?>">
            <div class="modal-header"><h5 class="modal-title">Novo Registro Clínico</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
            <div class="modal-body">
                <label class="form-label">Evolução clínica</label>
                <textarea class="form-control mb-3" name="evolucao" rows="5" minlength="10" required></textarea>
                <label class="form-label">Observações</label>
                <input class="form-control" name="observacoes">
                <small class="text-muted">Registros de prontuário não podem ser alterados depois (RN-PRT-04).</small>
            </div>
            <div class="modal-footer"><button type="submit" class="btn btn-primary">Registrar</button></div>
        </form>
    </div></div>
</div>
<?php endif; ?>

<div class="modal fade" id="modalPrescricao" tabindex="-1">
    <div class="modal-dialog"><div class="modal-content">
        <form method="POST" action="processar_prontuario.php">
            <input type="hidden" name="acao" value="prescrever"><input type="hidden" name="id_paciente" value="<?= $id ?>">
            <input type="hidden" name="id_prontuario" id="presc_id_prontuario">
            <div class="modal-header"><h5 class="modal-title">Nova Prescrição</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
            <div class="modal-body">
                <div class="mb-3"><label class="form-label">Medicamento</label>
                    <select class="form-select" name="id_medicamento" required>
                        <?php foreach ($medicamentos as $m): ?><option value="<?= (int) $m['id'] ?>"><?= e($m['nome']) ?></option><?php endforeach; ?>
                    </select>
                </div>
                <div class="row g-2">
                    <div class="col-4"><label class="form-label">Dosagem</label><input class="form-control" name="dosagem" placeholder="500mg" required></div>
                    <div class="col-4"><label class="form-label">Frequência</label><input class="form-control" name="frequencia" placeholder="8 em 8 horas" required></div>
                    <div class="col-4"><label class="form-label">Duração</label><input class="form-control" name="duracao" placeholder="7 dias" required></div>
                </div>
                <small class="text-muted d-block mt-2">A API verifica alergias e interações com prescrições dos últimos 30 dias.</small>
            </div>
            <div class="modal-footer"><button type="submit" class="btn btn-success">Prescrever</button></div>
        </form>
    </div></div>
</div>

<div class="modal fade" id="modalExame" tabindex="-1">
    <div class="modal-dialog"><div class="modal-content">
        <form method="POST" action="processar_prontuario.php">
            <input type="hidden" name="acao" value="exame"><input type="hidden" name="id_paciente" value="<?= $id ?>">
            <input type="hidden" name="id_prontuario" id="exame_id_prontuario">
            <div class="modal-header"><h5 class="modal-title">Solicitar Exame</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
            <div class="modal-body">
                <div class="mb-3"><label class="form-label">Exame</label><input class="form-control" name="nome_exame" placeholder="Hemograma completo" required></div>
                <div class="mb-3"><label class="form-label">Data</label><input type="date" class="form-control" name="data_exame" value="<?= date('Y-m-d') ?>"></div>
                <div><label class="form-label">Resultado (opcional)</label><textarea class="form-control" name="resultado" rows="2"></textarea></div>
            </div>
            <div class="modal-footer"><button type="submit" class="btn btn-primary">Salvar</button></div>
        </form>
    </div></div>
</div>

<script>
function abrirPrescricao(id) { document.getElementById('presc_id_prontuario').value = id; new bootstrap.Modal(document.getElementById('modalPrescricao')).show(); }
function abrirExame(id) { document.getElementById('exame_id_prontuario').value = id; new bootstrap.Modal(document.getElementById('modalExame')).show(); }
</script>
<?php require 'views/footer.php'; ?>
