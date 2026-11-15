namespace KunigiArchive.Application.Users;

// The password policy Identity enforces (Infrastructure's AddIdentity), as one message for the forms.
public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 100;

    public const string Message =
        "Ο κωδικός πρέπει να έχει τουλάχιστον 8 χαρακτήρες, με ένα κεφαλαίο, ένα πεζό γράμμα και ένα ψηφίο.";
}
