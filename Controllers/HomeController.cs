using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

using _9319_DistribuicaoLeads.Models;

using Dapper;

using Microsoft.AspNetCore.Mvc;
/*V1*/
namespace _9319_DistribuicaoLeads.Controllers 
{ 
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		private readonly IDbConnection _dbConnection;

		private readonly HttpClient _httpClient;

		public HomeController(ILogger<HomeController> logger, IDbConnection dbConnection, HttpClient httpClient)
		{
			_logger = logger;
			_dbConnection = dbConnection;
			_httpClient = httpClient;
		}
		public async Task<IActionResult> Index()
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

			var results = await _dbConnection.QueryAsync<AtribuirLeadDto>(consulta, commandTimeout: 560);

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
		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}