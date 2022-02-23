using Microsoft.EntityFrameworkCore;
using ShardingCore;
using ShardingCore.Bootstrapers;
using ShardingCore.TableExists;
using TwoPropertyDemo.Domain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
ILoggerFactory efLogger = LoggerFactory.Create(builder =>
{
    builder.AddFilter((category, level) => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information).AddConsole();
});
builder.Services.AddControllers();
builder.Services.AddShardingDbContext<MyDbContext>()
    .AddEntityConfig(o =>
    {
        o.CreateShardingTableOnStart = true;
        o.EnsureCreatedWithOutShardingTable = true;
        o.AddShardingTableRoute<DealRoute>();
    }).AddConfig(op =>
    {
        op.ConfigId = "c1";
        op.AddDefaultDataSource("ds0", "Data Source=localhost;Initial Catalog=TwoPropertyDemo;Integrated Security=True;");
        op.UseShardingQuery((conStr, b) =>
        {
            b.UseSqlServer(conStr).UseLoggerFactory(efLogger);
        });
        op.UseShardingTransaction((connection, b) =>
        {
            b.UseSqlServer(connection).UseLoggerFactory(efLogger);
        });
        op.ReplaceTableEnsureManager(sp=>new SqlServerTableEnsureManager<MyDbContext>());
    }).EnsureConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.Services.GetRequiredService<IShardingBootstrapper>().Start();
app.UseAuthorization();

app.MapControllers();

app.Run();
