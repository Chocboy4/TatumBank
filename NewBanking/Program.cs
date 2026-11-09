using NewBanking.Infrastructure;
using NewBanking.Presentation;
using NewBanking.Presentation.Banners;


InterBankSettings.Configure(args);

Banner.Display("Welcome to TatumBank!");

new BankApp().Run();