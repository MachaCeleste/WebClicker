namespace WebClicker;

public class Utils
{
    public static bool IsAlphaNumeric(string input)
    {
        return !string.IsNullOrEmpty(input) && input.All(char.IsLetterOrDigit);
    }

    public static bool IsNicknameValid(string nickName, out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrEmpty(nickName) || !Utils.IsAlphaNumeric(nickName) || nickName.Length < 2 || nickName.Length > 16)
        {
            reason = "Nickname must be alphanumeric and between 2-16 characters!";
            return false;
        }
        //else if (BadwordHandler.Singleton.ContainsBadwords(nickName))
        //{
        //    reason = "Nickname cannot contain bad words!";
        //    return false;
        //}
        return true;
    }
}
