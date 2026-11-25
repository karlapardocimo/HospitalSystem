<?php
require_once 'lib/bootstrap.php';
exigir_login();

if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('consultas.php');
$acao = $_POST['acao'] ?? '';
$id = (int) ($_POST['id'] ?? 0);

$r = match ($acao) {
    // POST /api/consultas
    'agendar' => api()->post('consultas', [
        'idPaciente' => (int) $_POST['id_paciente'],
        'idMedico' => (int) $_POST['id_medico'],
        'idSala' => nulo_se_vazio($_POST['id_sala'] ?? ''),
        'idConvenio' => nulo_se_vazio($_POST['id_convenio'] ?? ''),
        'data' => $_POST['data'],
        'horario' => hora_api($_POST['horario']),
        'enviarParaTriagem' => !empty($_POST['enviar_triagem']),
    ]),
    // POST /api/consultas/{id}/triagem  (a API calcula a classificação de Manchester)
    'triagem' => api()->post("consultas/$id/triagem", [
        'temperatura' => (float) str_replace(',', '.', $_POST['temp']),
        'pressaoArterial' => $_POST['pa'],
        'frequenciaCardiaca' => (int) $_POST['fc'],
        'saturacaoOxigenio' => (int) ($_POST['saturacao'] ?? 98),
        'escalaDor' => (int) ($_POST['dor'] ?? 0),
        'classificacaoRisco' => nulo_se_vazio($_POST['risco'] ?? ''),
        'queixaPrincipal' => nulo_se_vazio($_POST['queixa'] ?? ''),
    ]),
    // PATCH /api/consultas/{id}/...
    'enviar_triagem' => api()->patch("consultas/$id/enviar-triagem"),
    'cancelar' => api()->patch("consultas/$id/cancelar"),
    'nao_compareceu' => api()->patch("consultas/$id/nao-compareceu"),
    'liberar' => api()->patch("consultas/$id/liberar"),
    default => null,
};

if ($r) flash_api($r);
redirecionar(voltar_para('consultas.php'));
