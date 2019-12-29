module AllerRetour.Pbkdf2

open System
open System.Security.Cryptography
let private subkeyLength = 32
let private saltSize = 16

/// Hashes a password by a specified number of iterations using the PBKDF2 crypto function.
let hash password iterations =
  use algo = new Rfc2898DeriveBytes(password, saltSize, iterations)
  let salt = algo.Salt
  let bytes = algo.GetBytes(subkeyLength)

  let iters = if BitConverter.IsLittleEndian then BitConverter.GetBytes(iterations) else BitConverter.GetBytes(iterations) |> Array.rev

  let parts = Array.zeroCreate<byte> 54
  Buffer.BlockCopy(salt, 0, parts, 1, saltSize)
  Buffer.BlockCopy(bytes, 0, parts, 17, subkeyLength)
  Buffer.BlockCopy(iters, 0, parts, 50, sizeof<int>)

  Convert.ToBase64String(parts)

/// Hashes a password using 10,000 iterations of the PBKDF2 crypto function.
let fastHash password = hash password 10000

/// Hashes a password using 100,000 iterations of the PBKDF2 crypto function.
let strongHash password = hash password 100000

/// Hashes a password using 300,000 iterations of the PBKDF2 crypto function.
let uberHash password = hash password 300000

/// Verifies a PBKDF2 hashed password with a candidate password.
/// Returns true if the candidate password is correct.
/// The hashed password must have been originally generated by one of the hash functions within this module.
let verify hashedPassword (password:string) =
  let parts = Convert.FromBase64String(hashedPassword)
  if parts.Length <> 54 || parts.[0] <> byte 0 then
    false
  else
    let salt = Array.zeroCreate<byte> saltSize
    Buffer.BlockCopy(parts, 1, salt, 0, saltSize)

    let bytes = Array.zeroCreate<byte> subkeyLength
    Buffer.BlockCopy(parts, 17, bytes, 0, subkeyLength)

    let iters = Array.zeroCreate<byte> sizeof<int>
    Buffer.BlockCopy(parts, 50, iters, 0, sizeof<int>);

    let iters = if BitConverter.IsLittleEndian then iters else iters |> Array.rev

    let iterations = BitConverter.ToInt32(iters, 0)

    use algo = new Rfc2898DeriveBytes(password, salt, iterations)
    let challengeBytes = algo.GetBytes(32)

    match Seq.compareWith (fun a b -> if a = b then 0 else 1) bytes challengeBytes with
    | v when v = 0 -> true
    | _ -> false
