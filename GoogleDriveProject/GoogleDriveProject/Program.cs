using System.Runtime.InteropServices.JavaScript;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Sheets.v4;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Sheets.v4.Data;

namespace GoogleDriveProject
{
    class Program
    {
        private static readonly string CredentialsFilePath = "C:/Projects/Rider/TestReview/GoogleDriveProject/GoogleDriveProject/OAuthJSON/OAuth.json";
        private static readonly string SpreadsheetName = "mySpreadSheetTest";
        private static readonly string[] Scopes = { DriveService.Scope.DriveReadonly, SheetsService.Scope.Spreadsheets };
        private static DriveService DriveService;
        private static SheetsService SheetsService;
        private static Spreadsheet SpreadSheet;
        
        public static void Main(string[] args)
        {
            UserCredential credential = GetCredential();

            DriveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "driveApiTest"
            });
            
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "driveApiTest"
            }); 
            PrintResultList(DriveService);
            SpreadSheet = CreateSheet(SheetsService);
            Console.WriteLine("Sheet created with name: " + SpreadSheet.Properties.Title);
            Console.WriteLine("Updated list:");
            PrintResultList(DriveService);
            
            int num = 0; 
            TimerCallback timerCallback = new TimerCallback(SyncFilesToSpreadsheet);
            Timer timer = new Timer(timerCallback, num, 90000,90000);
            Console.ReadLine();
        }

        private static UserCredential GetCredential()
        {
            UserCredential credential;
            using (var stream = new FileStream(CredentialsFilePath, FileMode.Open, FileAccess.Read))
            {
                const string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            return credential;
        }

        private static void PrintResultList(DriveService driveService)
        {
            var req = driveService.Files.List();
            req.Q = "name contains '*'";
            var result = req.Execute();
            foreach (var item in result.Files)
            {
                Console.WriteLine("File: {0}", item.Name);
            }
        }

        private static Spreadsheet CreateSheet(SheetsService sheetsService)
        {
            return sheetsService.Spreadsheets.Create(new Spreadsheet()
            {
                Sheets = new List<Sheet>()
                {
                    new Sheet()
                    {
                        Properties = new SheetProperties()
                        {
                            Title = SpreadsheetName
                        }
                    }
                },
                Properties = new SpreadsheetProperties()
                {
                    Title = SpreadsheetName
                }
            }).Execute();
        }

        private static void SyncFilesToSpreadsheet(object? obj)
        {
            var request = DriveService.Files.List();
            request.Fields = "files(name, modifiedTime)";
            var files = request.Execute().Files;
            SheetsService.Spreadsheets.Values.Clear(new ClearValuesRequest(), SpreadSheet.SpreadsheetId, SpreadsheetName).Execute();
            var appendRequest = SheetsService.Spreadsheets.Values.Append(GetValueRange(files), SpreadSheet.SpreadsheetId, SpreadsheetName);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            appendRequest.Execute();

            Console.WriteLine("Sync process completed at: " + DateTime.Now);
            Console.WriteLine("Updated list after sync:");
            PrintResultList(DriveService);
        }

        private static ValueRange GetValueRange(IList<Google.Apis.Drive.v3.Data.File> files)
        {
            var rows = new List<IList<object>>();
            foreach (var file in files)
            {
                rows.Add(new List<object>(){file.Name, file.ModifiedTime.ToString()});
            }
            return new ValueRange
            {
                Values = rows
            };
        }
    }
}