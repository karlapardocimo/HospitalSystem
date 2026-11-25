<?php
// Cabeçalho comum. A página que inclui este arquivo já chamou lib/bootstrap.php e exigir_login().
$usuario = usuario_logado();
$paginaAtual = basename($_SERVER['PHP_SELF']);
$menu = function (string $arquivo, string $icone, string $titulo) use ($paginaAtual) {
    $ativo = $paginaAtual === $arquivo ? ' active' : '';
    echo "<li class=\"nav-item\"><a class=\"nav-link{$ativo}\" href=\"{$arquivo}\"><i class=\"fas {$icone} me-2\"></i> {$titulo}</a></li>";
};
?>
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= e(APP_NAME) ?></title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
    <style>
        :root { --primary-color: #2c3e50; --secondary-color: #34495e; --accent-color: #3498db; --bg-light: #f8f9fa; }
        body { background-color: var(--bg-light); font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
        .navbar { background-color: var(--primary-color) !important; }
        .sidebar { background-color: var(--secondary-color); min-height: calc(100vh - 56px); color: white; }
        .sidebar .nav-link { color: rgba(255,255,255,0.8); padding: 10px 20px; font-size: 0.9rem; }
        .sidebar .nav-link:hover, .sidebar .nav-link.active { color: white; background-color: rgba(255,255,255,0.1); }
        .sidebar .nav-heading { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05rem; padding: 15px 20px 5px; color: rgba(255,255,255,0.4); font-weight: bold; }
        .card { border: none; box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075); margin-bottom: 20px; }
        .card-header { background-color: white; border-bottom: 1px solid rgba(0,0,0,0.1); font-weight: bold; color: var(--primary-color); }
        .bg-orange { background-color: #fd7e14 !important; }
        .api-badge { font-size: .7rem; }
    </style>
</head>
<body>

<nav class="navbar navbar-expand-lg navbar-dark sticky-top">
    <div class="container-fluid">
        <a class="navbar-brand" href="index.php"><i class="fas fa-hospital-user me-2"></i> Manus Hospital</a>
        <span class="badge bg-info text-dark api-badge d-none d-md-inline" title="Endereço da API">API: <?= e(API_BASE_URL) ?></span>
        <ul class="navbar-nav ms-auto">
            <li class="nav-item dropdown">
                <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
                    <i class="fas fa-user-md me-1"></i> <?= e($usuario['nome'] ?? '') ?>
                    <span class="badge bg-light text-dark ms-1"><?= e($usuario['perfil'] ?? '') ?></span>
                </a>
                <ul class="dropdown-menu dropdown-menu-end">
                    <li><span class="dropdown-item-text small text-muted"><?= e($usuario['email'] ?? '') ?></span></li>
                    <li><span class="dropdown-item-text small text-muted">Sessão até <?= date('H:i', $_SESSION['token_expira'] ?? time()) ?></span></li>
                    <li><hr class="dropdown-divider"></li>
                    <li><a class="dropdown-item" href="logout.php"><i class="fas fa-sign-out-alt me-1"></i> Sair</a></li>
                </ul>
            </li>
        </ul>
    </div>
</nav>

<div class="container-fluid">
    <div class="row">
        <nav class="col-md-3 col-lg-2 d-md-block sidebar collapse p-0">
            <div class="position-sticky">
                <ul class="nav flex-column">
                    <?php $menu('index.php', 'fa-tachometer-alt', 'Dashboard'); ?>

                    <div class="nav-heading">Atendimento</div>
                    <?php $menu('pacientes.php', 'fa-user-injured', 'Pacientes'); ?>
                    <?php $menu('consultas.php', 'fa-calendar-check', 'Consultas & Triagem'); ?>

                    <div class="nav-heading">Clínico</div>
                    <?php $menu('medicos.php', 'fa-user-md', 'Médicos'); ?>
                    <?php $menu('medicamentos.php', 'fa-pills', 'Medicamentos'); ?>

                    <div class="nav-heading">Operacional</div>
                    <?php $menu('leitos.php', 'fa-bed', 'Gestão de Leitos'); ?>
                    <?php $menu('internacoes.php', 'fa-procedures', 'Internações'); ?>
                    <?php $menu('estoque.php', 'fa-boxes', 'Estoque Insumos'); ?>

                    <div class="nav-heading">Administrativo</div>
                    <?php $menu('convenios.php', 'fa-id-card', 'Convênios'); ?>
                    <?php $menu('salas.php', 'fa-door-open', 'Salas'); ?>
                    <?php if (pode('Administrador', 'Recepcao')) $menu('faturamento.php', 'fa-file-invoice-dollar', 'Faturamento'); ?>
                    <?php if (pode('Administrador')) $menu('usuarios.php', 'fa-users-cog', 'Usuários'); ?>
                </ul>
            </div>
        </nav>
        <main class="col-md-9 ms-sm-auto col-lg-10 px-md-4 py-4">
            <?php render_flash(); ?>
