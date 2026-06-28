using Agenda.Domain;

namespace Agenda.Api;

public static class AuthExtensions {
	public static TBuilder RequiereAuth<TBuilder>(this TBuilder builder)
		where TBuilder : IEndpointConventionBuilder {
		return builder.AddEndpointFilter(async (context, next) => {
			var usuario = context.HttpContext.Items["Usuario"] as Usuario;
			if (usuario is null) {
				return Results.Unauthorized();
			}

			return await next(context);
		});
	}
}