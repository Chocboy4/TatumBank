using NewBanking.Infrastructure;
using NewBanking.Presentation;


InterBankSettings.Configure(args);

new BankApp().Run();