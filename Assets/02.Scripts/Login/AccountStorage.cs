using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// 역할 : 회원 계정(아이디·비밀번호)을 파일로 저장하고, 가입·로그인을 확인한다.
// 저장 위치는 Application.persistentDataPath/accounts.json 이고, 비밀번호는 그대로 두지 않고 해시(SHA-256)로 바꿔 저장한다.
public static class AccountStorage
{
    private const string FileName = "accounts.json";

    // 아이디 : 이메일 주소이거나, 영문·숫자·-·@·_ 로 된 3~20 글자
    private static readonly Regex IdRegex = new Regex("^[A-Za-z0-9\\-@_]{3,20}$");
    private static readonly Regex EmailRegex = new Regex("^[A-Za-z0-9._%+\\-]+@[A-Za-z0-9.\\-]+\\.[A-Za-z]{2,}$");

    // 비밀번호 : 대문자·소문자·숫자·기호를 하나 이상씩 넣은 8~30 글자 (공백·한글 등은 안 된다)
    private static readonly Regex PasswordRegex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9])[!-~]{8,30}$");

    private static AccountList _accounts; // 한 번 읽은 계정 목록 (처음 쓸 때 파일에서 읽는다)

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // 아이디 형식이 맞는지
    public static bool IsValidId(string id)
    {
        return string.IsNullOrEmpty(id) == false && (IdRegex.IsMatch(id) || EmailRegex.IsMatch(id));
    }

    // 비밀번호 형식이 맞는지
    public static bool IsValidPassword(string password)
    {
        return string.IsNullOrEmpty(password) == false && PasswordRegex.IsMatch(password);
    }

    public static bool Exists(string id)
    {
        return Load().Accounts.Exists(account => account.Id == id);
    }

    // 새 계정을 저장한다. 이미 있는 아이디면 false.
    public static bool TryRegister(string id, string password)
    {
        if (Exists(id)) return false;

        AccountList accounts = Load();
        accounts.Accounts.Add(new Account { Id = id, PasswordHash = Hash(id, password) });
        File.WriteAllText(FilePath, JsonUtility.ToJson(accounts, true));
        return true;
    }

    // 아이디와 비밀번호가 저장된 계정과 맞는지
    public static bool Verify(string id, string password)
    {
        Account account = Load().Accounts.Find(saved => saved.Id == id);
        return account != null && account.PasswordHash == Hash(id, password);
    }

    private static AccountList Load()
    {
        if (_accounts != null) return _accounts;

        _accounts = File.Exists(FilePath)
            ? JsonUtility.FromJson<AccountList>(File.ReadAllText(FilePath))
            : new AccountList();
        return _accounts;
    }

    // 같은 비밀번호라도 아이디마다 다른 값이 나오도록 아이디를 섞어서 해시한다.
    private static string Hash(string id, string password)
    {
        using SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(id + ":" + password));
        return BitConverter.ToString(bytes).Replace("-", string.Empty);
    }

    [Serializable]
    private class Account
    {
        public string Id;
        public string PasswordHash;
    }

    [Serializable]
    private class AccountList
    {
        public List<Account> Accounts = new List<Account>();
    }
}
