-- Script completo do banco (também executado automaticamente pela API na inicialização).
CREATE DATABASE IF NOT EXISTS hospital CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE hospital;

-- =====================================================================
-- Sistema de Gestão Hospitalar - schema v2 (API ASP.NET Core)
-- Idempotente: pode ser executado várias vezes sem apagar dados.
-- Mantém as tabelas e colunas do sistema PHP e acrescenta:
--   * tabela usuarios (autenticação JWT)
--   * triagem.id_consulta / data_triagem / queixa_principal / classificacao_calculada
--   * prontuario.profissional
--   * prescricoes.data_prescricao / alerta_confirmado
--   * faturamento.data_pagamento
--   * uso_insumos.id_internacao
-- =====================================================================

CREATE TABLE IF NOT EXISTS usuarios (
    id_usuario INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    perfil VARCHAR(20) NOT NULL,
    ativo TINYINT(1) NOT NULL DEFAULT 1,
    tentativas_falhas INT NOT NULL DEFAULT 0,
    bloqueado_ate DATETIME NULL,
    criado_em DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS pacientes (
    id_paciente INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(100) NOT NULL,
    cpf VARCHAR(14) UNIQUE NOT NULL,
    data_nascimento DATE,
    telefone VARCHAR(20),
    endereco VARCHAR(150),
    alergias TEXT,
    historico_clinico TEXT,
    tipo_sanguineo VARCHAR(5)
);

CREATE TABLE IF NOT EXISTS medicos (
    id_medico INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(100) NOT NULL,
    crm VARCHAR(20) UNIQUE NOT NULL,
    especialidade VARCHAR(50),
    telefone VARCHAR(20),
    email VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS convenios (
    id_convenio INT PRIMARY KEY AUTO_INCREMENT,
    nome_convenio VARCHAR(100),
    validade_carteira DATE,
    telefone VARCHAR(20)
);

CREATE TABLE IF NOT EXISTS salas (
    id_sala INT PRIMARY KEY AUTO_INCREMENT,
    numero_sala VARCHAR(10),
    tipo_sala VARCHAR(50),
    status_sala VARCHAR(20)
);

CREATE TABLE IF NOT EXISTS consultas (
    id_consulta INT PRIMARY KEY AUTO_INCREMENT,
    data_consulta DATE,
    horario TIME,
    status_consulta VARCHAR(30),
    id_paciente INT,
    id_medico INT,
    id_sala INT,
    id_convenio INT,
    FOREIGN KEY (id_paciente) REFERENCES pacientes(id_paciente),
    FOREIGN KEY (id_medico) REFERENCES medicos(id_medico),
    FOREIGN KEY (id_sala) REFERENCES salas(id_sala),
    FOREIGN KEY (id_convenio) REFERENCES convenios(id_convenio)
);

CREATE TABLE IF NOT EXISTS triagem (
    id_triagem INT PRIMARY KEY AUTO_INCREMENT,
    temperatura DECIMAL(4,1),
    pressao_arterial VARCHAR(20),
    frequencia_cardiaca INT,
    saturacao_oxigenio INT,
    escala_dor INT,
    classificacao_risco VARCHAR(20),
    classificacao_calculada VARCHAR(20),
    protocolo_manchester VARCHAR(30),
    queixa_principal TEXT,
    data_triagem DATETIME,
    id_paciente INT,
    id_consulta INT,
    FOREIGN KEY (id_paciente) REFERENCES pacientes(id_paciente),
    FOREIGN KEY (id_consulta) REFERENCES consultas(id_consulta)
);

CREATE TABLE IF NOT EXISTS prontuario (
    id_prontuario INT PRIMARY KEY AUTO_INCREMENT,
    evolucao_clinica TEXT,
    observacoes TEXT,
    data_registro DATETIME,
    profissional VARCHAR(150),
    id_consulta INT,
    FOREIGN KEY (id_consulta) REFERENCES consultas(id_consulta)
);

CREATE TABLE IF NOT EXISTS exames (
    id_exame INT PRIMARY KEY AUTO_INCREMENT,
    nome_exame VARCHAR(100),
    resultado TEXT,
    data_exame DATE,
    id_prontuario INT,
    FOREIGN KEY (id_prontuario) REFERENCES prontuario(id_prontuario)
);

CREATE TABLE IF NOT EXISTS medicamentos (
    id_medicamento INT PRIMARY KEY AUTO_INCREMENT,
    nome_medicamento VARCHAR(100),
    contraindicacoes TEXT,
    interacoes_medicamentosas TEXT
);

CREATE TABLE IF NOT EXISTS prescricoes (
    id_prescricao INT PRIMARY KEY AUTO_INCREMENT,
    dosagem VARCHAR(50),
    frequencia VARCHAR(50),
    duracao_tratamento VARCHAR(50),
    data_prescricao DATETIME,
    alerta_confirmado TEXT,
    id_prontuario INT,
    id_medicamento INT,
    FOREIGN KEY (id_prontuario) REFERENCES prontuario(id_prontuario),
    FOREIGN KEY (id_medicamento) REFERENCES medicamentos(id_medicamento)
);

CREATE TABLE IF NOT EXISTS leitos (
    id_leito INT PRIMARY KEY AUTO_INCREMENT,
    numero_leito VARCHAR(10),
    ala VARCHAR(50),
    status_leito VARCHAR(20),
    tempo_higienizacao INT
);

CREATE TABLE IF NOT EXISTS internacoes (
    id_internacao INT PRIMARY KEY AUTO_INCREMENT,
    data_entrada DATETIME,
    data_alta DATETIME,
    status_internacao VARCHAR(30),
    id_paciente INT,
    id_leito INT,
    FOREIGN KEY (id_paciente) REFERENCES pacientes(id_paciente),
    FOREIGN KEY (id_leito) REFERENCES leitos(id_leito)
);

CREATE TABLE IF NOT EXISTS insumos (
    id_insumo INT PRIMARY KEY AUTO_INCREMENT,
    nome_insumo VARCHAR(100),
    quantidade_estoque INT,
    valor_unitario DECIMAL(10,2)
);

CREATE TABLE IF NOT EXISTS uso_insumos (
    id_uso INT PRIMARY KEY AUTO_INCREMENT,
    quantidade_utilizada INT,
    data_uso DATETIME,
    id_leito INT,
    id_insumo INT,
    id_internacao INT,
    FOREIGN KEY (id_leito) REFERENCES leitos(id_leito),
    FOREIGN KEY (id_insumo) REFERENCES insumos(id_insumo),
    FOREIGN KEY (id_internacao) REFERENCES internacoes(id_internacao)
);

CREATE TABLE IF NOT EXISTS faturamento (
    id_faturamento INT PRIMARY KEY AUTO_INCREMENT,
    valor_total DECIMAL(10,2),
    data_faturamento DATE,
    status_pagamento VARCHAR(30),
    data_pagamento DATE,
    id_paciente INT,
    FOREIGN KEY (id_paciente) REFERENCES pacientes(id_paciente)
);

CREATE TABLE IF NOT EXISTS auditoria (
    id_auditoria INT PRIMARY KEY AUTO_INCREMENT,
    descricao TEXT,
    data_auditoria DATETIME,
    status_auditoria VARCHAR(30),
    id_faturamento INT,
    FOREIGN KEY (id_faturamento) REFERENCES faturamento(id_faturamento)
);
