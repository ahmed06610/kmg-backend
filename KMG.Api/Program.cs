using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using KMG.Api.Authorization;
using KMG.Core.Authorization;
using KMG.Core.Helper;
using KMG.Core.Interfaces;
using KMG.Core.Interfaces.Services;
using KMG.Core.Models;
using KMG.Core.Services;
using KMG.EF.Data;
using KMG.EF.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, RoleIdentity>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ICashBoxService, CashBoxService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IMissionService, MissionService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAiContextService, AiContextService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ApiKeyAuthFilter>();

builder.Services.Configure<JWT>(builder.Configuration.GetSection("JWT"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = false;
    o.SaveToken = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)),
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"]
    };
});

builder.Services.AddScoped<IAuthorizationHandler, AbilityAuthorizationHandler>();

// Policies بتاعة "Ability:*" بتتبني ديناميكيًا وقت الطلب (مش وقت الإقلاع) عشان تشتغل صح
// من أول تشغيل حتى لو الداتابيز لسه فاضية وقت الـ Startup، وعشان أي Ability جديدة تتضاف
// بعد كده تشتغل على طول من غير الحاجة لإعادة تشغيل السيرفر.
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, AbilityPolicyProvider>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KMG API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "أدخل التوكين فقط بدون Bearer",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtSecurityScheme, Array.Empty<string>() } });
});

var app = builder.Build();

// معالج أخطاء عام - بيمنع تسريب الـ Stack Trace وهيدرز الطلب (زي الـ JWT) للعميل،
// وبيرجع نفس شكل الرسالة (ex.Message) اللي الكونترولرز التانية بترجعه من الـ try/catch بتاعها يدويًا،
// عشان أي Endpoint نسي try/catch (زي SupplierController.RecordPayment) يفضل آمن برضه
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new { message = exception?.Message ?? "حدث خطأ غير متوقع" });
    });
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<KMG.EF.Data.ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await KMG.Api.Helper.DbInitializer.SeedAsync(services);

    if (app.Environment.IsDevelopment())
        await KMG.Api.Helper.DummyDataSeeder.SeedAsync(app.Services);
}


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseStaticFiles(); // لخدمة مرفقات المشاريع المرفوعة تحت wwwroot/uploads
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
