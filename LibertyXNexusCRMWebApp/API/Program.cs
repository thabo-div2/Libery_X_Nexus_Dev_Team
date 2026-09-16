using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public partial class Program
    {
        public static async Task Main(string[] args) 
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Replace("[DataPath]", dataDirectory);

            builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlServer(connectionString));



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
