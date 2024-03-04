SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE UNLOGGED TABLE cliente (
    id integer PRIMARY KEY NOT NULL,
    saldo integer NOT NULL,
    limite integer NOT NULL
);

CREATE UNLOGGED TABLE transacao (
    id SERIAL PRIMARY KEY,
    valor integer NOT NULL,
    descricao varchar(10) NOT NULL,
    realizadaem timestamp NOT NULL DEFAULT (NOW() at time zone 'utc'),
    idcliente integer NOT NULL
);

CREATE INDEX ix_transacao_idcliente ON transacao
(
    idcliente ASC
);

delete from transacao;
delete from cliente;

INSERT INTO cliente (id, saldo, limite) VALUES (1, 0, -100000);
INSERT INTO cliente (id, saldo, limite) VALUES (2, 0, -80000);
INSERT INTO cliente (id, saldo, limite) VALUES (3, 0, -1000000);
INSERT INTO cliente (id, saldo, limite) VALUES (4, 0, -10000000);
INSERT INTO cliente (id, saldo, limite) VALUES (5, 0, -500000);