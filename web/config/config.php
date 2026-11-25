<?php
// Configurações do aplicativo (cliente da API).
// O aplicativo NÃO acessa mais o banco de dados: tudo passa pela API ASP.NET Core.

date_default_timezone_set(getenv('TZ') ?: 'America/Sao_Paulo');

// Endereço da API. Pode ser sobrescrito pela variável de ambiente API_BASE_URL (usada no Docker).
define('API_BASE_URL', rtrim(getenv('API_BASE_URL') ?: 'http://localhost:5080/api', '/'));
define('API_TIMEOUT', 15); // segundos

define('APP_NAME', 'Sistema de Gestão Hospitalar');
define('APP_VERSION', '2.0.0');

// Listas usadas nos formulários (a validação oficial é feita pela API)
$TIPOS_SANGUINEOS = ['O+', 'O-', 'A+', 'A-', 'B+', 'B-', 'AB+', 'AB-'];
$ESPECIALIDADES = ['Cardiologia', 'Neurologia', 'Ortopedia', 'Pediatria', 'Ginecologia', 'Oftalmologia',
    'Otorrinolaringologia', 'Dermatologia', 'Psiquiatria', 'Cirurgia Geral', 'Clínica Geral'];
$PERFIS = ['Administrador', 'Medico', 'Enfermagem', 'Recepcao'];
$TIPOS_SALA = ['Consultório', 'Cirurgia', 'Exame', 'Triagem', 'Internação'];
$CLASSIFICACOES = [
    'Azul' => 'Azul (Não Urgente)',
    'Verde' => 'Verde (Pouco Urgente)',
    'Amarelo' => 'Amarelo (Urgente)',
    'Laranja' => 'Laranja (Muito Urgente)',
    'Vermelho' => 'Vermelho (Emergência)',
];
