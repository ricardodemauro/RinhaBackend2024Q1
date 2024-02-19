CREATE TABLE `rinha-2024`.`clientes`
  (
     `id`     INT NOT NULL,
     `nome`   VARCHAR(50) NULL,
     `limite` INT NULL,
     `saldo`  INT NULL
  )

CREATE TABLE `rinha-2024`.`transacoes`
  (
     `id`           INT NOT NULL auto_increment,
     `id_cliente`   INT NULL,
     `valor`        INT NULL,
     `tipo`         CHAR(1) NULL,
     `descricao`    VARCHAR(10) NULL,
     `realizado_em` TIMESTAMP ON UPDATE CURRENT_TIMESTAMP NULL,
     PRIMARY KEY (`id`)
  )