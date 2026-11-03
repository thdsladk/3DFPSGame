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

    // 영문자(대/소문자)와 숫자만 쓰고, 영문자와 숫자가 둘 다 들어가야 한다. (아이디·비밀번호 공통 규칙)
    private static readonly Regex FormatRegex = new Regex("^(?=.*[A-Za-z])(?=.*[0-9])[A-Za-z0-9]+$");

    private static AccountList _accounts; // 한 번 읽은 계정 목록 (처음 쓸 때 파일에서 읽는다)

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // 아이디·비밀번호 형식이 맞는지
    public static bool IsValidFormat(string text)
    {
        return string.IsNullOrEmpty(text) == false && FormatRegex.IsMatch(text);
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
