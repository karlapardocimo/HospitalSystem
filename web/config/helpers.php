<?php
// Funções utilitárias do aplicativo (somente apresentação).
// As validações de negócio (CPF, CRM, Manchester, interações...) agora ficam na API.

/** Escapa texto para HTML (evita XSS). */
function e($valor): string
{
    return htmlspecialchars((string) ($valor ?? ''), ENT_QUOTES, 'UTF-8');
}

function formatarCPF(?string $cpf): string
{
    $cpf = preg_replace('/\D/', '', (string) $cpf);
    return strlen($cpf) === 11 ? preg_replace('/(\d{3})(\d{3})(\d{3})(\d{2})/', '$1.$2.$3-$4', $cpf) : (string) $cpf;
}

function formatarTelefone(?string $telefone): string
{
    $t = preg_replace('/\D/', '', (string) $telefone);
    if (strlen($t) === 11) return preg_replace('/(\d{2})(\d{5})(\d{4})/', '($1) $2-$3', $t);
    if (strlen($t) === 10) return preg_replace('/(\d{2})(\d{4})(\d{4})/', '($1) $2-$3', $t);
    return (string) $telefone;
}

function formatarData(?string $data): string
{
    if (!$data || str_starts_with($data, '0001-01-01')) return '-';
    return (new DateTime($data))->format('d/m/Y');
}

function formatarDataHora(?string $dataHora): string
{
    if (!$dataHora) return '-';
    return (new DateTime($dataHora))->format('d/m/Y H:i');
}

function formatarHora(?string $hora): string
{
    return $hora ? substr($hora, 0, 5) : '-';
}

function formatarMoeda($valor): string
{
    return 'R$ ' . number_format((float) $valor, 2, ',', '.');
}

/** Cor do Bootstrap para cada classificação de Manchester. */
function classeRisco(?string $risco): string
{
    return match ($risco) {
        'Vermelho' => 'bg-danger',
        'Laranja' => 'bg-orange text-white',
        'Amarelo' => 'bg-warning text-dark',
        'Verde' => 'bg-success',
        'Azul' => 'bg-primary',
        default => 'bg-secondary',
    };
}

function classeStatusLeito(?string $status): string
{
    return match ($status) {
        'Ocupado' => 'danger',
        'Higienização' => 'warning',
        'Manutenção' => 'secondary',
        default => 'success',
    };
}

// ------------------------------------------------------------------ Mensagens (flash) entre redirecionamentos

function flash(string $tipo, string $mensagem, array $erros = []): void
{
    $_SESSION['flash'][] = ['tipo' => $tipo, 'mensagem' => $mensagem, 'erros' => $erros];
}

/** Guarda a resposta da API como mensagem para a próxima página. */
function flash_api(ApiResponse $r, ?string $mensagemSucesso = null): void
{
    if ($r->success) {
        flash('success', $mensagemSucesso ?? $r->message);
    } else {
        flash($r->status === 0 ? 'warning' : 'danger', $r->message, $r->errors);
    }
}

function render_flash(): void
{
    foreach ($_SESSION['flash'] ?? [] as $f) {
        echo '<div class="alert alert-' . e($f['tipo']) . ' alert-dismissible fade show" role="alert">';
        echo '<strong>' . e($f['mensagem']) . '</strong>';
        if (!empty($f['erros'])) {
            echo '<ul class="mb-0 mt-1 small">';
            foreach ($f['erros'] as $erro) echo '<li>' . e($erro) . '</li>';
            echo '</ul>';
        }
        echo '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>';
    }
    unset($_SESSION['flash']);
}

// ------------------------------------------------------------------ Reaproveitar dados do formulário após erro

function guardar_formulario(array $dados): void
{
    $_SESSION['old'] = $dados;
}

function old(string $campo, $padrao = '')
{
    return $_SESSION['old'][$campo] ?? $padrao;
}

function limpar_formulario(): void
{
    unset($_SESSION['old']);
}

/** Converte "" em null (campos opcionais do formulário). */
function nulo_se_vazio($valor)
{
    return ($valor === '' || $valor === null) ? null : $valor;
}

/** Garante horário no formato HH:MM:SS exigido pela API. */
function hora_api(string $hora): string
{
    return strlen($hora) === 5 ? $hora . ':00' : $hora;
}

function redirecionar(string $url): never
{
    header('Location: ' . $url);
    exit;
}

/** Página de retorno informada pelo formulário, aceitando só arquivos locais (evita redirecionamento aberto). */
function voltar_para(string $padrao): string
{
    $voltar = $_POST['voltar'] ?? '';
    return preg_match('/^[a-z_]+\.php(\?[a-z_]+=\d+)?$/', $voltar) ? $voltar : $padrao;
}
