namespace LostAndFound.Api.Email;

/// <summary>The password reset email, in Arabic and English so it reads correctly whatever the user's language.</summary>
public static class PasswordResetEmail
{
    public static EmailMessage Create(string toAddress, string toName, string resetLink) => new(
        toAddress,
        toName,
        "Reset your Findly password | إعادة تعيين كلمة مرور Findly",
        $"""
        Hello {toName},

        We received a request to reset your Findly password. Open this link to choose a new one:
        {resetLink}

        The link expires in 1 hour. If you did not ask for this, ignore this email and your password stays the same.

        ---

        مرحبًا {toName}،

        وصلنا طلب لإعادة تعيين كلمة مرور حسابك على Findly. افتح الرابط التالي لاختيار كلمة مرور جديدة:
        {resetLink}

        تنتهي صلاحية الرابط خلال ساعة. إذا لم تطلب ذلك، تجاهل هذه الرسالة وستبقى كلمة المرور كما هي.
        """);
}
