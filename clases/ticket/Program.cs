using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Agenda.Api;
using Agenda.Data;
using Agenda.Domain;
using Agenda.Repositories;
using Agenda.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => {
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<TicketContext>(options =>
	options.UseSqlite("Data Source=tickets.db"));

builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
	var context = scope.ServiceProvider.GetRequiredService<TicketContext>();
	await context.Database.EnsureCreatedAsync();

	if (app.Environment.IsDevelopment()) {
		await SeedData.SembrarAsync(context);
	}
}

app.Use(async (context, next) => {
	var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
	var cronometro = Stopwatch.StartNew();

	logger.LogInformation("-> {Metodo} {Ruta}", context.Request.Method, context.Request.Path);

	await next(context);

	cronometro.Stop();
	logger.LogInformation(
		"<- {Metodo} {Ruta} respondio {Status} en {Ms}ms",
		context.Request.Method,
		context.Request.Path,
		context.Response.StatusCode,
		cronometro.ElapsedMilliseconds);
});

app.Use(async (context, next) => {
	var auth = context.RequestServices.GetRequiredService<AuthService>();
	var header = context.Request.Headers.Authorization.ToString();

	if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
		var token = header["Bearer ".Length..].Trim();
		var usuario = await auth.ValidarTokenAsync(token);

		if (usuario is not null) {
			context.Items["Usuario"] = usuario;
		}
	}

	await next(context);
});

var authGroup = app.MapGroup("/auth");

authGroup.MapPost("/registro", async (RegistroDto dto, AuthService auth) => {
	try {
		var usuario = await auth.RegistrarAsync(dto.Nombre, dto.Email, dto.Password, dto.Tipo);
		return Results.Created($"/usuarios/{usuario.Id}", new {
			usuario.Id,
			usuario.Nombre,
			usuario.Email,
			usuario.Tipo
		});
	}
	catch (InvalidOperationException ex) {
		return Results.BadRequest(new { error = ex.Message });
	}
});

authGroup.MapPost("/login", async (LoginDto dto, AuthService auth) => {
	var token = await auth.LoginAsync(dto.Email, dto.Password);
	return token is null ? Results.Unauthorized() : Results.Ok(new { token });
});

authGroup.MapPost("/logout", async (HttpContext ctx, AuthService auth) => {
	var usuario = ctx.UsuarioActual()!;
	await auth.LogoutAsync(usuario.Token!);
	return Results.Ok(new { mensaje = "Sesion cerrada." });
})
.RequiereAuth();

var ticketsGroup = app.MapGroup("/tickets")
	.RequiereAuth();

ticketsGroup.MapGet("/", async (TicketService svc) => {
	var tickets = await svc.ListarTicketsAsync();
	return Results.Ok(tickets.Select(MapearTicket));
});

ticketsGroup.MapGet("/{id:int}", async (int id, TicketService svc) => {
	var ticket = await svc.ObtenerTicketAsync(id);
	return ticket is null ? Results.NotFound() : Results.Ok(MapearTicket(ticket));
});

ticketsGroup.MapGet("/estado/{estado}", async (EstadoTicket estado, TicketService svc) => {
	var tickets = await svc.ListarPorEstadoAsync(estado);
	return Results.Ok(tickets.Select(MapearTicket));
});

ticketsGroup.MapPost("/", async (CrearTicketDto dto, HttpContext ctx, TicketService svc) => {
	var usuario = ctx.UsuarioActual()!;
	var ticket = await svc.CrearTicketAsync(
		dto.Titulo,
		dto.Descripcion,
		usuario.Id,
		dto.ResponsableId);

	return Results.Created($"/tickets/{ticket.Id}", MapearTicket(ticket));
});

ticketsGroup.MapPut("/{id:int}/estado", async (int id, CambiarEstadoDto dto, TicketService svc) => {
	try {
		await svc.CambiarEstadoAsync(id, dto.Estado);
		return Results.NoContent();
	}
	catch (InvalidOperationException ex) {
		return Results.NotFound(new { error = ex.Message });
	}
});

ticketsGroup.MapPut("/{id:int}/responsable", async (int id, AsignarResponsableDto dto, TicketService svc) => {
	try {
		await svc.AsignarResponsableAsync(id, dto.ResponsableId);
		return Results.NoContent();
	}
	catch (InvalidOperationException ex) {
		return Results.NotFound(new { error = ex.Message });
	}
});

ticketsGroup.MapPost("/{id:int}/acciones", async (
	int id,
	RegistrarAccionDto dto,
	HttpContext ctx,
	TicketService svc) => {
	var usuario = ctx.UsuarioActual()!;

	try {
		var accion = await svc.RegistrarAccionAsync(id, dto.Descripcion, usuario.Id, dto.Fecha);
		return Results.Created($"/tickets/{id}/acciones/{accion.Id}", new {
			accion.Id,
			accion.Descripcion,
			accion.Fecha,
			accion.Realizada
		});
	}
	catch (InvalidOperationException ex) {
		return Results.NotFound(new { error = ex.Message });
	}
});

ticketsGroup.MapPut("/{ticketId:int}/acciones/{accionId:int}/realizada", async (
	int ticketId,
	int accionId,
	TicketService svc) => {
	try {
		await svc.MarcarAccionRealizadaAsync(ticketId, accionId);
		return Results.NoContent();
	}
	catch (InvalidOperationException ex) {
		return Results.NotFound(new { error = ex.Message });
	}
});

app.Run();

static object MapearTicket(Ticket ticket) => new {
	ticket.Id,
	ticket.Titulo,
	ticket.Descripcion,
	ticket.Estado,
	ticket.FechaCreacion,
	OriginadoPor = ticket.OriginadoPor?.Nombre,
	Responsable = ticket.Responsable?.Nombre,
	Acciones = ticket.Acciones.Select(accion => new {
		accion.Id,
		accion.Descripcion,
		accion.Fecha,
		accion.Realizada
	})
};
