<?php
require_once 'lib/bootstrap.php';
exigir_login();

if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('pacientes.php');
$idPaciente = (int) ($_POST['id_paciente'] ?? 0);
$acao = $_POST['acao'] ?? '';

switch ($acao) {
    case 'evolucao': // POST /api/consultas/{id}/prontuario
        $r = api()->post('consultas/' . (int) $_POST['id_consulta'] . '/prontuario', [
            'evolucaoClinica' => $_POST['evolucao'] ?? '',
            'observacoes' => nulo_se_vazio($_POST['observacoes'] ?? ''),
        ]);
        flash_api($r);
        break;

    case 'prescrever': // POST /api/prontuarios/{id}/prescricoes
        $idProntuario = (int) $_POST['id_prontuario'];
        $r = api()->post("prontuarios/$idProntuario/prescricoes", [
            'idMedicamento' => (int) $_POST['id_medicamento'],
            'dosagem' => $_POST['dosagem'] ?? '',
            'frequencia' => $_POST['frequencia'] ?? '',
            'duracaoTratamento' => $_POST['duracao'] ?? '',
            'confirmarInteracao' => !empty($_POST['confirmar_interacao']),
        ]);

        $interacao = !$r->success && array_filter($r->errors, fn($e) => str_starts_with($e, 'RN-PRE-04'));
        if ($interacao) {
            // Guarda a prescrição para o médico confirmar conscientemente na próxima tela.
            $_SESSION['prescricao_pendente'] = [
                'id_prontuario' => $idProntuario, 'id_medicamento' => (int) $_POST['id_medicamento'],
                'dosagem' => $_POST['dosagem'], 'frequencia' => $_POST['frequencia'], 'duracao' => $_POST['duracao'],
                'alertas' => $r->errors,
            ];
        } elseif ($r->success && !empty($r->data['alertas'])) {
            flash('success', $r->message, $r->data['alertas']);
        } else {
            flash_api($r);
        }
        break;

    case 'exame': // POST /api/prontuarios/{id}/exames
        $r = api()->post('prontuarios/' . (int) $_POST['id_prontuario'] . '/exames', [
            'nomeExame' => $_POST['nome_exame'] ?? '',
            'dataExame' => nulo_se_vazio($_POST['data_exame'] ?? ''),
            'resultado' => nulo_se_vazio($_POST['resultado'] ?? ''),
        ]);
        flash_api($r);
        break;

    case 'resultado': // PATCH /api/exames/{id}/resultado
        $r = api()->patch('exames/' . (int) $_POST['id_exame'] . '/resultado', ['resultado' => $_POST['resultado'] ?? '']);
        flash_api($r);
        break;
}

redirecionar("prontuario.php?id=$idPaciente");
