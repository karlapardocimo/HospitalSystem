-- Dados de demonstração (inseridos só quando o banco está vazio e Database:CriarDadosDemo = true)

INSERT INTO medicos (nome, crm, especialidade, telefone, email) VALUES
 ('Dra. Helena Souza', '123456', 'Clínica Geral', '32999990001', 'helena.souza@hospital.local'),
 ('Dr. Ricardo Alves', '234567', 'Cardiologia', '32999990002', 'ricardo.alves@hospital.local'),
 ('Dra. Marina Costa', '345678', 'Pediatria', '32999990003', 'marina.costa@hospital.local');

INSERT INTO convenios (nome_convenio, validade_carteira, telefone) VALUES
 ('Unimed', '2030-12-31', '08000000001'),
 ('Bradesco Saúde', '2030-12-31', '08000000002'),
 ('Amil', '2024-12-31', '08000000003');

INSERT INTO salas (numero_sala, tipo_sala, status_sala) VALUES
 ('01', 'Consultório', 'Disponível'),
 ('02', 'Consultório', 'Disponível'),
 ('T1', 'Triagem', 'Disponível'),
 ('C1', 'Cirurgia', 'Manutenção');

INSERT INTO leitos (numero_leito, ala, status_leito, tempo_higienizacao) VALUES
 ('101', 'Ala Norte', 'Disponível', 30),
 ('102', 'Ala Norte', 'Disponível', 30),
 ('103', 'Ala Norte', 'Higienização', 30),
 ('104', 'Ala Norte', 'Manutenção', 30),
 ('UTI-01', 'UTI', 'Disponível', 45),
 ('UTI-02', 'UTI', 'Disponível', 45);

INSERT INTO medicamentos (nome_medicamento, contraindicacoes, interacoes_medicamentosas) VALUES
 ('Aspirina', 'Úlcera péptica ativa; distúrbios de coagulação', 'Ibuprofeno, Naproxeno, Warfarina'),
 ('Ibuprofeno', 'Insuficiência renal grave; úlcera ativa', 'Aspirina, Warfarina'),
 ('Warfarina', 'Gestação; sangramento ativo', 'Aspirina, Ibuprofeno'),
 ('Metformina', 'Insuficiência renal; acidose metabólica', 'Álcool'),
 ('Dipirona', 'Discrasias sanguíneas', NULL),
 ('Amoxicilina', 'Alergia a penicilinas', NULL),
 ('Naproxeno', 'Úlcera ativa', 'Aspirina');

INSERT INTO insumos (nome_insumo, quantidade_estoque, valor_unitario) VALUES
 ('Seringa 10 ml', 150, 0.85),
 ('Luva de procedimento (par)', 500, 0.45),
 ('Gaze estéril', 15, 1.20),
 ('Soro fisiológico 500 ml', 80, 6.50),
 ('Cateter venoso periférico', 12, 9.90);

INSERT INTO pacientes (nome, cpf, data_nascimento, telefone, endereco, alergias, historico_clinico, tipo_sanguineo) VALUES
 ('João da Silva', '52601815906', '1985-05-20', '32988887777', 'Rua das Flores, 123 - Centro, Muriaé - MG', 'Dipirona', 'Hipertenso', 'O+'),
 ('Maria Oliveira', '08301661305', '1992-10-12', '32977776666', 'Av. Brasil, 450 - Barra, Muriaé - MG', NULL, NULL, 'A-'),
 ('Pedro Santos', '18609139034', '2015-03-08', '32966665555', 'Rua Padre Antônio, 77 - Muriaé - MG', 'Amoxicilina', 'Asma leve', 'B+');
