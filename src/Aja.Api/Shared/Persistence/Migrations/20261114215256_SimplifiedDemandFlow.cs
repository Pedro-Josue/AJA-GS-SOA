using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aja.Api.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifiedDemandFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorrespondenciasLegadas",
                columns: table => new
                {
                    OrigemTipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrigemId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DestinoTipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DestinoId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tratamento = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrespondenciasLegadas", x => new { x.OrigemTipo, x.OrigemId, x.DestinoTipo, x.DestinoId });
                });

            migrationBuilder.CreateTable(
                name: "Demandas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    Local = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EvidenciaUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    AutorId = table.Column<string>(type: "TEXT", nullable: false),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: true),
                    Situacao = table.Column<int>(type: "INTEGER", nullable: false),
                    Visibilidade = table.Column<int>(type: "INTEGER", nullable: false),
                    Demonstrativa = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrigemLegada = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Removida = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemovidaPorId = table.Column<string>(type: "TEXT", nullable: true),
                    RemovidaEm = table.Column<long>(type: "INTEGER", nullable: true),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demandas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Demandas_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demandas_AspNetUsers_RemovidaPorId",
                        column: x => x.RemovidaPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comentarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemandaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AutorId = table.Column<string>(type: "TEXT", nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 3000, nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    Removido = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemovidoPorId = table.Column<string>(type: "TEXT", nullable: true),
                    RemovidoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    Oculto = table.Column<bool>(type: "INTEGER", nullable: false),
                    MotivoModeracao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ModeradoPorId = table.Column<string>(type: "TEXT", nullable: true),
                    ModeradoEm = table.Column<long>(type: "INTEGER", nullable: true),
                    TipoLegado = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NaturezaLegada = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Demonstrativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrigemLegada = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comentarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comentarios_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comentarios_AspNetUsers_ModeradoPorId",
                        column: x => x.ModeradoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comentarios_AspNetUsers_RemovidoPorId",
                        column: x => x.RemovidoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comentarios_Demandas_DemandaId",
                        column: x => x.DemandaId,
                        principalTable: "Demandas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandaApoios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemandaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    AtualizadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    OrigemLegada = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandaApoios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandaApoios_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandaApoios_Demandas_DemandaId",
                        column: x => x.DemandaId,
                        principalTable: "Demandas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricosSituacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DemandaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SituacaoAnterior = table.Column<int>(type: "INTEGER", nullable: false),
                    SituacaoNova = table.Column<int>(type: "INTEGER", nullable: false),
                    Justificativa = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ResponsavelId = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEm = table.Column<long>(type: "INTEGER", nullable: false),
                    OrigemLegada = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Demonstrativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosSituacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricosSituacao_AspNetUsers_ResponsavelId",
                        column: x => x.ResponsavelId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoricosSituacao_Demandas_DemandaId",
                        column: x => x.DemandaId,
                        principalTable: "Demandas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Conversão conservadora: as tabelas anteriores permanecem intactas e cada
            // registro recebe uma correspondência persistente para o destino ou para a
            // estrutura histórica em que continua preservado.
            migrationBuilder.Sql(
                """
                INSERT INTO Demandas
                    (Id, Descricao, Local, EvidenciaUrl, AutorId, Cidade, Bairro, Categoria, Situacao,
                     Visibilidade, Demonstrativa, OrigemLegada, Removida, RemovidaPorId, RemovidaEm,
                     CriadoEm, AtualizadoEm)
                SELECT
                    Id, Contexto, ReferenciaLocal, NULL, ResponsavelId, Cidade, Bairro, Categoria,
                    CASE WHEN Situacao = 1 THEN 1 ELSE 2 END,
                    CASE WHEN Situacao = 5 THEN 3 ELSE 1 END,
                    Demonstrativo, 'Topico', 0, NULL, NULL, CriadoEm, AtualizadoEm
                FROM Topicos;

                INSERT INTO Demandas
                    (Id, Descricao, Local, EvidenciaUrl, AutorId, Cidade, Bairro, Categoria, Situacao,
                     Visibilidade, Demonstrativa, OrigemLegada, Removida, RemovidaPorId, RemovidaEm,
                     CriadoEm, AtualizadoEm)
                SELECT
                    r.Id, r.Descricao, r.ReferenciaLocal,
                    (SELECT e.Url FROM Evidencias e
                     WHERE e.RelatoId = r.Id AND e.Url IS NOT NULL AND
                           (lower(e.Url) LIKE 'http://%' OR lower(e.Url) LIKE 'https://%')
                     ORDER BY e.CriadoEm, e.Id LIMIT 1),
                    r.AutorId, r.Cidade, r.Bairro, r.Categoria, 1,
                    CASE WHEN r.Oculto = 1 THEN 2 ELSE 1 END,
                    r.Demonstrativo, 'Relato', 0, NULL, NULL, r.CriadoEm, r.AtualizadoEm
                FROM Relatos r
                WHERE r.TopicoId IS NULL;

                INSERT INTO DemandaApoios
                    (Id, DemandaId, UsuarioId, Ativo, CriadoEm, AtualizadoEm, OrigemLegada)
                SELECT Id, TopicoId, UsuarioId, 1, CriadoEm, CriadoEm, 'Apoio'
                FROM Apoios;

                INSERT INTO Comentarios
                    (Id, DemandaId, AutorId, Texto, CriadoEm, Removido, RemovidoPorId, RemovidoEm,
                     Oculto, MotivoModeracao, ModeradoPorId, ModeradoEm, TipoLegado, NaturezaLegada,
                     Demonstrativo, OrigemLegada)
                SELECT
                    Id, TopicoId, AutorId, Texto, CriadoEm, 0, NULL, NULL, Oculta,
                    MotivoModeracao, ModeradaPorId, ModeradaEm,
                    CASE Tipo WHEN 1 THEN 'Comentario' WHEN 2 THEN 'Observacao'
                              WHEN 3 THEN 'Alternativa' WHEN 4 THEN 'Avaliacao' ELSE 'Desconhecido' END,
                    CASE Natureza WHEN 1 THEN 'FatoObservado' WHEN 2 THEN 'InformacaoInferida'
                                  WHEN 3 THEN 'BeneficioEsperado' ELSE 'Desconhecida' END,
                    Demonstrativo, 'Participacao'
                FROM Participacoes;

                INSERT INTO HistoricosSituacao
                    (Id, DemandaId, SituacaoAnterior, SituacaoNova, Justificativa, ResponsavelId,
                     CriadoEm, OrigemLegada, Demonstrativo)
                SELECT
                    Id, TopicoId,
                    CASE WHEN SituacaoAnterior = 1 THEN 1 ELSE 2 END,
                    CASE WHEN SituacaoNova = 1 THEN 1 ELSE 2 END,
                    Descricao, AutorId, CriadoEm, 'Acompanhamento', Simulado
                FROM Acompanhamentos
                WHERE (CASE WHEN SituacaoAnterior = 1 THEN 1 ELSE 2 END) <>
                      (CASE WHEN SituacaoNova = 1 THEN 1 ELSE 2 END);

                INSERT INTO CorrespondenciasLegadas
                    (OrigemTipo, OrigemId, DestinoTipo, DestinoId, Tratamento)
                SELECT 'AspNetUser', Id, 'AspNetUser', Id, 'Preservado no lugar, inclusive ID e hash'
                FROM AspNetUsers;

                INSERT INTO CorrespondenciasLegadas
                    (OrigemTipo, OrigemId, DestinoTipo, DestinoId, Tratamento)
                SELECT 'Topico', CAST(Id AS TEXT), 'Demanda', CAST(Id AS TEXT),
                       CASE WHEN Situacao = 5
                            THEN 'Convertido em demanda histórica não pública'
                            ELSE 'Convertido em demanda coletiva' END
                FROM Topicos;

                INSERT INTO CorrespondenciasLegadas
                    (OrigemTipo, OrigemId, DestinoTipo, DestinoId, Tratamento)
                SELECT 'Relato', CAST(Id AS TEXT), 'Demanda',
                       CAST(COALESCE(TopicoId, Id) AS TEXT),
                       CASE WHEN TopicoId IS NULL
                            THEN 'Convertido em demanda própria; origem integral preservada'
                            ELSE 'Preservado no agrupamento do tópico' END
                FROM Relatos;

                INSERT INTO CorrespondenciasLegadas
                    (OrigemTipo, OrigemId, DestinoTipo, DestinoId, Tratamento)
                SELECT 'Evidencia', CAST(e.Id AS TEXT), 'Demanda',
                       CAST(COALESCE(r.TopicoId, r.Id) AS TEXT),
                       'Preservada integralmente na tabela histórica; uma URL pode ser projetada no relato isolado'
                FROM Evidencias e
                JOIN Relatos r ON r.Id = e.RelatoId;

                INSERT INTO CorrespondenciasLegadas
                    (OrigemTipo, OrigemId, DestinoTipo, DestinoId, Tratamento)
                SELECT 'Apoio', CAST(Id AS TEXT), 'DemandaApoio', CAST(Id AS TEXT),
                       'Convertido uma única vez com o mesmo ID'
                FROM Apoios;

                INSERT INTO CorrespondenciasLegadas
                    (OrigemTipo, OrigemId, DestinoTipo, DestinoId, Tratamento)
                SELECT 'Participacao', CAST(Id AS TEXT), 'Comentario', CAST(Id AS TEXT),
                       'Convertida uma única vez; tipo, natureza e moderação preservados'
                FROM Participacoes;

                INSERT INTO CorrespondenciasLegadas
                    (OrigemTipo, OrigemId, DestinoTipo, DestinoId, Tratamento)
                SELECT 'Dossie', CAST(Id AS TEXT), 'DossieLegado', CAST(Id AS TEXT),
                       'Snapshot e versão preservados somente para leitura administrativa'
                FROM Dossies;

                INSERT INTO CorrespondenciasLegadas
                    (OrigemTipo, OrigemId, DestinoTipo, DestinoId, Tratamento)
                SELECT 'Acompanhamento', CAST(a.Id AS TEXT),
                       CASE WHEN h.Id IS NULL THEN 'AcompanhamentoLegado' ELSE 'HistoricoSituacao' END,
                       CAST(a.Id AS TEXT),
                       CASE WHEN h.Id IS NULL
                            THEN 'Preservado sem inventar transição no fluxo simplificado'
                            ELSE 'Convertido em mudança de situação equivalente' END
                FROM Acompanhamentos a
                LEFT JOIN HistoricosSituacao h ON h.Id = a.Id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_AutorId",
                table: "Comentarios",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_DemandaId_CriadoEm",
                table: "Comentarios",
                columns: new[] { "DemandaId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_ModeradoPorId",
                table: "Comentarios",
                column: "ModeradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_RemovidoPorId",
                table: "Comentarios",
                column: "RemovidoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandaApoios_UsuarioId",
                table: "DemandaApoios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_DemandaApoio_Demanda_Usuario",
                table: "DemandaApoios",
                columns: new[] { "DemandaId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_AutorId",
                table: "Demandas",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_RemovidaPorId",
                table: "Demandas",
                column: "RemovidaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_Situacao_Categoria_Bairro_CriadoEm",
                table: "Demandas",
                columns: new[] { "Situacao", "Categoria", "Bairro", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosSituacao_DemandaId_CriadoEm",
                table: "HistoricosSituacao",
                columns: new[] { "DemandaId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosSituacao_ResponsavelId",
                table: "HistoricosSituacao",
                column: "ResponsavelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comentarios");

            migrationBuilder.DropTable(
                name: "CorrespondenciasLegadas");

            migrationBuilder.DropTable(
                name: "DemandaApoios");

            migrationBuilder.DropTable(
                name: "HistoricosSituacao");

            migrationBuilder.DropTable(
                name: "Demandas");
        }
    }
}
