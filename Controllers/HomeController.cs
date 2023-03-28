using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using _9319_DistribuicaoLeads.Models;

using CsvHelper;
using CsvHelper.Configuration;

using Dapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

using NPOI.SS.Formula.Functions;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
/*V1*/
namespace _9319_DistribuicaoLeads.Controllers 
{ 
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

        private readonly IDbConnection _sqlConnection;
        private readonly IDbConnection _excelConnection;

        private readonly HttpClient _httpClient;

		public HomeController(ILogger<HomeController> logger, IDbConnection excelConnection, IDbConnection sqlConnection, HttpClient httpClient)
        {   
			_logger = logger;
            _sqlConnection = sqlConnection;
            _excelConnection = excelConnection;
            _httpClient = httpClient;

        }
		public IActionResult Index()
		{
			return View();
		}
		public async Task<IActionResult> Privacy()
		{

            var consulta =
            @"
			Use us_crm
            DECLARE @identificadorUsuarioExterno VARCHAR(10);
            DECLARE @CountInten INT; 
            SET @CountInten = 1;

            SET @identificadorUsuarioExterno = '10783';

            declare @tab TABLE (LeadId int, UsuarioId int, identificadorUsuarioExterno VARCHAR(12))
            declare @tabhunter TABLE (CONTADOR_ID INT IDENTITY PRIMARY KEY, hunter_id_crm int) 

            insert into @tabhunter 
	            select id from  Usuario us
	            where id in (596, 549, 48, 42, 55, 35, 21);


            while @CountInten <= (select COUNT(hunter_id_crm) from @tabhunter)
            BEGIN 
	            INSERT @tab 
			            select top(10)  le.id_crm as LeadId,  
                        (select hunter_id_crm from @tabhunter where CONTADOR_ID = @CountInten), 
                        @identificadorUsuarioExterno AS identificadorUsuarioExterno
			            from DB_processo_precatorio_lead le
            inner join pjus_qa_2..Processo pr
            on pr.IdBdCrm = le.id
            inner join pjus_qa_2..Pessoa pe
            on pr.Cliente_Id = pe.Id
            where le.num_processo = '0417062-63.1999.8.26.0053'
            and pe.UsuarioHunter_Id is null
            and le.cod_status = 24
			            and id_crm NOT IN (select LeadId from @tab)
			            order by le.id;
		            set @CountInten = @CountInten + 1;
            END
		            select * from @tab";

            var results = await _sqlConnection.QueryAsync<AtribuirLeadDto>(consulta, commandTimeout: 560);


            foreach (var result in results)
            {
                var ItemJson = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json");

                using var httpResponseMessage = await _httpClient.PutAsync("https://integracaocrm.api.qa.pjus.com.br/api/Leads/atribuir-lead", ItemJson);

                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    break;
                }
            }

            return View();
		}

        public ActionResult AtualizacaoProcessosFederais()
        {
            var csvPath = @"C:\Users\keveen.menezes\OneDrive - PJUS INVESTIMENTOS EM DIREITOS CREDITORIOS LTDA\Área de Trabalho\updateSisPjus2.csv";
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = false,
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, csvConfig))
            {
                var records = csv.GetRecords<UpdateSispjus>().ToList();
                return View("CsvData", records);
            }

        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}