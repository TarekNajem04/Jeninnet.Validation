//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorComponents();
builder.Services.AddSingleton<IValidator<ProductViewModel>, ProductValidator>();
builder.Services.AddScoped<ValidationFilter<ProductViewModel>>();
ValidationCatalogServiceCollectionExtensions.AddValidation(builder.Services);

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<Jeninnet.Validation.Sample.Mvc.Components.App>();
await app.RunAsync();
