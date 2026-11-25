<?php
require_once 'lib/bootstrap.php';
exigir_login();

$api = api();
$filaTriagem = $api->get('consultas/fila-triagem');          // status "Aguardando Triagem"
$filaAtendimento = $api->get('consultas/fila-atendimento');  // ordenada por Manchester na API
$agendadas = $api->get('consultas', ['status' => 'Agendada']);
$liberados = $api->get('consultas', ['status' => 'Liberado']);
foreach ([$filaTriagem, $filaAtendimento] as $r) if (!$r->success) flash_api($r);

$podeAgendar = pode('Administrador', 'Recepcao');
if ($podeAgendar) {
    $pacientes = $api->get('pacientes')->data ?? [];
    $medicos = $api->get('medicos')->data ?? [];       // cache na API
    $salas = $api->get('salas')->data ?? [];           // cache na API
    $convenios = $api->get('convenios')->data ?? [];   // cache na API
}

require 'views/header.php';
?>
<div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Agendamento & Triagem</h1>
    <?php if ($podeAgendar): ?>
    <button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#modalAgendamento">
        <i class="fas fa-calendar-plus me-1"></i> Novo Agendamento
    </button>
    <?php endif; ?>
</div>

<div class="row g-4">
    <!-- Fila de espera para triagem -->
    <div class="col-12 col-xl-4">
        <div class="card border-0 shadow-sm h-100">
            <div class="card-header bg-primary text-white py-3 border-0">
                <h6 class="m-0"><i class="fas fa-list-ol me-2"></i>Fila de Espera para Triagem</h6>
            </div>
            <div class="card-body p-0">
                <div class="list-group list-group-flush">
                <?php foreach ($filaTriagem->data ?? [] as $f): ?>
                    <div class="list-group-item d-flex justify-content-between align-items-center p-3">
                        <div>
                            <h6 class="mb-0"><?= e($f['pacienteNome']) ?></h6>
                            <small class="text-muted">Consulta #<?= (int) $f['idConsulta'] ?> · <?= formatarData($f['data']) ?> <?= formatarHora($f['horario']) ?><?= $f['medicoNome'] ? ' · ' . e($f['medicoNome']) : '' ?></small>
                        </div>
                        <div class="btn-group">
                            <?php if (pode('Administrador', 'Medico', 'Enfermagem')): ?>
                            <button class="btn btn-sm btn-success" onclick="abrirTriagem('<?= e(addslashes($f['pacienteNome'])) ?>', <?= (int) $f['idConsulta'] ?>)">Triar</button>
                            <?php endif; ?>
                            <?php if ($podeAgendar): ?>
                            <form method="POST" action="processar_consultas.php" onsubmit="return confirm('Cancelar esta consulta?')">
                                <input type="hidden" name="acao" value="cancelar"><input type="hidden" name="id" value="<?= (int) $f['idConsulta'] ?>">
                                <button class="btn btn-sm btn-outline-danger" title="Cancelar"><i class="fas fa-times"></i></button>
                            </form>
                            <?php endif; ?>
                        </div>
                    </div>
                <?php endforeach; ?>
                <?php if (empty($filaTriagem->data)): ?>
                    <div class="p-5 text-center text-muted">
                        <i class="fas fa-check-circle fa-3x mb-3 text-success opacity-50"></i>
                        <p class="mb-0">Fila vazia. Todos os pacientes triados.</p>
                    </div>
                <?php endif; ?>
                </div>
            </div>
        </div>
    </div>

    <div class="col-12 col-xl-8">
        <div class="card border-0 shadow-sm">
            <div class="card-header bg-white border-0 py-3">
                <ul class="nav nav-pills card-header-pills" role="tablist">
                    <li class="nav-item"><button class="nav-link active" data-bs-toggle="tab" data-bs-target="#fila" type="button">Fila de Atendimento (<?= count($filaAtendimento->data ?? []) ?>)</button></li>
                    <li class="nav-item"><button class="nav-link" data-bs-toggle="tab" data-bs-target="#agendadas" type="button">Agendadas (<?= count($agendadas->data ?? []) ?>)</button></li>
                    <li class="nav-item"><button class="nav-link" data-bs-toggle="tab" data-bs-target="#historico" type="button">Pacientes Liberados</button></li>
                </ul>
            </div>
            <div class="card-body p-0">
                <div class="tab-content">
                    <!-- Fila de atendimento (Protocolo de Manchester) -->
                    <div class="tab-pane fade show active" id="fila">
                        <div class="table-responsive">
                            <table class="table table-hover align-middle mb-0">
                                <thead class="table-light">
                                    <tr><th class="ps-4">Paciente</th><th>Prioridade</th><th>Sinais Vitais</th><th>Espera</th><th class="text-end pe-4">Ações</th></tr>
                                </thead>
                                <tbody>
                                <?php foreach ($filaAtendimento->data ?? [] as $t): ?>
                                    <tr class="<?= $t['classificacaoRisco'] === 'Vermelho' ? 'table-danger fw-bold' : '' ?>">
                                        <td class="ps-4">
                                            <strong><?= e($t['pacienteNome']) ?></strong><br>
                                            <small class="text-muted">Consulta #<?= (int) $t['idConsulta'] ?><?= $t['queixaPrincipal'] ? ' · ' . e($t['queixaPrincipal']) : '' ?></small>
                                        </td>
                                        <td><span class="badge <?= classeRisco($t['classificacaoRisco']) ?> w-100"><?= e($t['classificacaoRisco']) ?></span></td>
                                        <td class="small">PA: <?= e($t['pressaoArterial']) ?> · Sat: <?= (int) $t['saturacaoOxigenio'] ?>%<br>
                                            T: <?= e($t['temperatura']) ?>°C · FC: <?= (int) $t['frequenciaCardiaca'] ?> · Dor: <?= (int) $t['escalaDor'] ?></td>
                                        <td class="small">
                                            <?= (int) $t['minutosAguardando'] ?> min / alvo <?= (int) $t['tempoAlvoMinutos'] ?> min
                                            <?php if ($t['atrasado']): ?><br><span class="badge bg-danger">Atrasado</span><?php endif; ?>
                                        </td>
                                        <td class="text-end pe-4">
                                            <div class="btn-group">
                                                <a href="prontuario.php?id=<?= (int) $t['idPaciente'] ?>" class="btn btn-sm btn-outline-primary">Atender</a>
                                                <?php if (pode('Administrador', 'Medico')): ?>
                                                <form method="POST" action="processar_consultas.php" onsubmit="return confirm('Liberar este paciente? Ele será movido para o histórico.')">
                                                    <input type="hidden" name="acao" value="liberar"><input type="hidden" name="id" value="<?= (int) $t['idConsulta'] ?>">
                                                    <button class="btn btn-sm btn-outline-success">Liberar</button>
                                                </form>
                                                <?php endif; ?>
                                            </div>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                                <?php if (empty($filaAtendimento->data)): ?>
                                    <tr><td colspan="5" class="text-center py-5 text-muted">Nenhum paciente em atendimento.</td></tr>
                                <?php endif; ?>
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <!-- Consultas agendadas -->
                    <div class="tab-pane fade" id="agendadas">
                        <div class="table-responsive">
                            <table class="table table-hover align-middle mb-0">
                                <thead class="table-light"><tr><th class="ps-4">Paciente</th><th>Data/Hora</th><th>Médico</th><th>Sala / Convênio</th><th class="text-end pe-4">Ações</th></tr></thead>
                                <tbody>
                                <?php foreach ($agendadas->data ?? [] as $c): ?>
                                    <tr>
                                        <td class="ps-4"><strong><?= e($c['pacienteNome']) ?></strong></td>
                                        <td><?= formatarData($c['data']) ?> <?= formatarHora($c['horario']) ?></td>
                                        <td><?= e($c['medicoNome'] ?? '-') ?></td>
                                        <td class="small"><?= e($c['salaNumero'] ?? '-') ?> / <?= e($c['convenioNome'] ?? 'Particular') ?></td>
                                        <td class="text-end pe-4">
                                            <?php if ($podeAgendar): ?>
                                            <div class="btn-group">
                                                <?php foreach (['enviar_triagem' => ['Check-in', 'btn-outline-primary'], 'nao_compareceu' => ['Faltou', 'btn-outline-secondary'], 'cancelar' => ['Cancelar', 'btn-outline-danger']] as $acao => [$rotulo, $cls]): ?>
                                                <form method="POST" action="processar_consultas.php">
                                                    <input type="hidden" name="acao" value="<?= $acao ?>"><input type="hidden" name="id" value="<?= (int) $c['id'] ?>">
                                                    <button class="btn btn-sm <?= $cls ?>"><?= $rotulo ?></button>
                                                </form>
                                                <?php endforeach; ?>
                                            </div>
                                            <?php endif; ?>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                                <?php if (empty($agendadas->data)): ?>
                                    <tr><td colspan="5" class="text-center py-5 text-muted">Nenhuma consulta agendada.</td></tr>
                                <?php endif; ?>
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <!-- Histórico de liberados -->
                    <div class="tab-pane fade" id="historico">
                        <div class="table-responsive">
                            <table class="table table-hover align-middle mb-0">
                                <thead class="table-light"><tr><th class="ps-4">Paciente</th><th>Data/Hora</th><th>Médico</th><th class="text-end pe-4">Ações</th></tr></thead>
                                <tbody>
                                <?php foreach ($liberados->data ?? [] as $h): ?>
                                    <tr>
                                        <td class="ps-4"><strong><?= e($h['pacienteNome']) ?></strong></td>
                                        <td><?= formatarData($h['data']) ?> <?= formatarHora($h['horario']) ?></td>
                                        <td><?= e($h['medicoNome'] ?? '-') ?></td>
                                        <td class="text-end pe-4"><a href="prontuario.php?id=<?= (int) $h['idPaciente'] ?>" class="btn btn-sm btn-outline-secondary">Ver Prontuário</a></td>
                                    </tr>
                                <?php endforeach; ?>
                                <?php if (empty($liberados->data)): ?>
                                    <tr><td colspan="4" class="text-center py-5 text-muted">Nenhum histórico de liberação disponível.</td></tr>
                                <?php endif; ?>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Modal de Triagem -->
<div class="modal fade" id="modalTriagem" tabindex="-1">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <form action="processar_consultas.php" method="POST">
                <input type="hidden" name="acao" value="triagem">
                <input type="hidden" name="id" id="triagem_id_consulta">
                <div class="modal-header">
                    <h5 class="modal-title">Realizar Triagem: <span id="nomePacienteTriagem"></span></h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <div class="row g-3">
                        <div class="col-md-4"><label class="form-label">Temperatura (°C)</label><input type="number" step="0.1" class="form-control" name="temp" required></div>
                        <div class="col-md-4"><label class="form-label">Pressão Arterial</label><input type="text" class="form-control" name="pa" placeholder="120/80" required></div>
                        <div class="col-md-4"><label class="form-label">Freq. Cardíaca (bpm)</label><input type="number" class="form-control" name="fc" required></div>
                        <div class="col-md-4"><label class="form-label">Saturação O₂ (%)</label><input type="number" class="form-control" name="saturacao" value="98" min="50" max="100" required></div>
                        <div class="col-md-4"><label class="form-label">Escala de Dor (0-10)</label><input type="number" class="form-control" name="dor" min="0" max="10" value="0"></div>
                        <div class="col-md-4">
                            <label class="form-label">Classificação (Manchester)</label>
                            <select class="form-select" name="risco">
                                <option value="">Calcular pelos sinais vitais</option>
                                <?php foreach ($CLASSIFICACOES as $valor => $rotulo): ?><option value="<?= $valor ?>"><?= $rotulo ?></option><?php endforeach; ?>
                            </select>
                            <small class="text-muted">A API calcula o risco; só é possível elevar.</small>
                        </div>
                        <div class="col-12"><label class="form-label">Queixa Principal / Observações</label><textarea class="form-control" name="queixa" rows="3"></textarea></div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancelar</button>
                    <button type="submit" class="btn btn-primary">Finalizar Triagem</button>
                </div>
            </form>
        </div>
    </div>
</div>

<?php if ($podeAgendar): ?>
<!-- Modal de Agendamento -->
<div class="modal fade" id="modalAgendamento" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <form action="processar_consultas.php" method="POST">
                <input type="hidden" name="acao" value="agendar">
                <div class="modal-header"><h5 class="modal-title">Novo Agendamento</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label">Paciente</label>
                        <select class="form-select" name="id_paciente" required>
                            <option value="">Escolha um paciente...</option>
                            <?php foreach ($pacientes as $p): ?><option value="<?= (int) $p['id'] ?>"><?= e($p['nome']) ?> (<?= e($p['cpfFormatado']) ?>)</option><?php endforeach; ?>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Médico</label>
                        <select class="form-select" name="id_medico" required>
                            <option value="">Escolha um médico...</option>
                            <?php foreach ($medicos as $m): ?><option value="<?= (int) $m['id'] ?>"><?= e($m['nome']) ?> - <?= e($m['especialidade']) ?></option><?php endforeach; ?>
                        </select>
                    </div>
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label class="form-label">Sala</label>
                            <select class="form-select" name="id_sala">
                                <option value="">Sem sala definida</option>
                                <?php foreach ($salas as $s): ?><option value="<?= (int) $s['id'] ?>"><?= e($s['numero']) ?> - <?= e($s['tipo']) ?><?= $s['status'] !== 'Disponível' ? ' (' . e($s['status']) . ')' : '' ?></option><?php endforeach; ?>
                            </select>
                        </div>
                        <div class="col-md-6 mb-3">
                            <label class="form-label">Convênio</label>
                            <select class="form-select" name="id_convenio">
                                <option value="">Particular (Sem Convênio)</option>
                                <?php foreach ($convenios as $c): ?><option value="<?= (int) $c['id'] ?>"><?= e($c['nome']) ?><?= $c['vencido'] ? ' (vencido)' : '' ?></option><?php endforeach; ?>
                            </select>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6 mb-3"><label class="form-label">Data</label><input type="date" class="form-control" name="data" value="<?= date('Y-m-d') ?>" required></div>
                        <div class="col-md-6 mb-3"><label class="form-label">Horário</label><input type="time" class="form-control" name="horario" value="<?= date('H:i', strtotime('+15 minutes')) ?>" required></div>
                    </div>
                    <div class="form-check">
                        <input class="form-check-input" type="checkbox" name="enviar_triagem" id="checkTriagem" value="1" checked>
                        <label class="form-check-label" for="checkTriagem">Enviar para Fila de Triagem</label>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancelar</button>
                    <button type="submit" class="btn btn-primary">Confirmar</button>
                </div>
            </form>
        </div>
    </div>
</div>
<?php endif; ?>

<script>
function abrirTriagem(nome, idConsulta) {
    document.getElementById('nomePacienteTriagem').innerText = nome;
    document.getElementById('triagem_id_consulta').value = idConsulta;
    new bootstrap.Modal(document.getElementById('modalTriagem')).show();
}
</script>
<?php require 'views/footer.php'; ?>
