<?php
// Antes: INSERT/UPDATE/DELETE direto no MySQL. Agora: chamadas REST à API.
require_once 'lib/bootstrap.php';
exigir_login();

if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('pacientes.php');
$acao = $_POST['acao'] ?? '';

if ($acao === 'excluir') {
    $r = api()->delete('pacientes/' . (int) $_POST['id']);       // DELETE /api/pacientes/{id}
    flash_api($r);
    redirecionar('pacientes.php');
}

$dados = [
    'nome' => trim($_POST['nome'] ?? ''),
    'cpf' => $_POST['cpf'] ?? '',
    'dataNascimento' => $_POST['data_nascimento'] ?: null,
    'telefone' => nulo_se_vazio($_POST['telefone'] ?? ''),
    'endereco' => nulo_se_vazio($_POST['endereco'] ?? ''),
    'alergias' => nulo_se_vazio($_POST['alergias'] ?? ''),
    'historicoClinico' => nulo_se_vazio($_POST['historico_clinico'] ?? ''),
    'tipoSanguineo' => nulo_se_vazio($_POST['tipo_sanguineo'] ?? ''),
];

if ($acao === 'editar') {
    $id = (int) $_POST['id_paciente'];
    $r = api()->put("pacientes/$id", $dados);                    // PUT /api/pacientes/{id}
    flash_api($r);
    if ($r->success) redirecionar('pacientes.php');
    guardar_formulario($_POST);
    redirecionar("paciente_editar.php?id=$id");
}

$dados['enviarParaTriagem'] = !empty($_POST['enviar_triagem']);
$r = api()->post('pacientes', $dados);                           // POST /api/pacientes
flash_api($r);
if ($r->success) {
    redirecionar($dados['enviarParaTriagem'] ? 'consultas.php' : 'pacientes.php');
}
guardar_formulario($_POST);
redirecionar('paciente_novo.php');
