using System.Linq.Expressions;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.Specifications;

namespace GLaDOS.OldschoolRunescape.Specifications;

public class OsrsUserWithUsername : SpecificationBase<OldschoolRunescapeUser>
{
    private readonly string _username;

    public OsrsUserWithUsername(string username)
    {
        _username = username;
    }
    public override Expression<Func<OldschoolRunescapeUser, bool>> Criteria =>
        osrsUser => osrsUser.Username.ToLower() == _username.ToLower();
}