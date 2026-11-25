using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HospitalSystem.Application.Abstractions;
using HospitalSystem.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HospitalSystem.Infrastructure.Security;

/// <summary>
/// Gera o JWT assinado com HMAC-SHA256.
/// Estrutura: header.payload.assinatura (Base64Url). O payload carrega as claims abaixo;
/// a assinatura com a chave secreta garante que o token não foi alterado pelo cliente.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtSettings> opcoes, TimeProvider relogio) : ITokenService
{
    private readonly JwtSettings _cfg = opcoes.Value;

    public TokenGerado Gerar(Usuario usuario)
    {
        var agoraUtc = relogio.GetUtcNow().UtcDateTime;
        var expiraUtc = agoraUtc.AddMinutes(_cfg.ExpiracaoMinutos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("role", usuario.Perfil),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())  // id único do token
        };

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _cfg.Issuer,
            audience: _cfg.Audience,
            claims: claims,
            notBefore: agoraUtc,
            expires: expiraUtc,
            signingCredentials: credenciais);

        return new TokenGerado(new JwtSecurityTokenHandler().WriteToken(token), expiraUtc.ToLocalTime());
    }
}
