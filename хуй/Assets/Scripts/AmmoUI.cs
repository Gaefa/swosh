// using TMPro;
// using UnityEngine;
//
// public class AmmoUI : MonoBehaviour
// {
//     public MobileShoot playerShoot;
//     public TMP_Text currentText;
//     public TMP_Text reserveText;
//
//     private void Start()
//     {
//         if (playerShoot == null)
//             playerShoot = FindObjectOfType<MobileShoot>();
//
//         UpdateUI();
//     }
//
//     private void Update()
//     {
//         UpdateUI();
//     }
//
//     private void UpdateUI()
//     {
//         if (playerShoot == null) return;
//
//         // playerShoot сейчас хранит ammo внутри private
//         // поэтому мы берём строку ammoText "10/40" и парсим (быстро для MVP)
//         if (playerShoot.ammoText == null) return;
//
//         string s = playerShoot.ammoText.text; // "10/40"
//         if (string.IsNullOrEmpty(s)) return;
//
//         var parts = s.Split('/');
//         if (parts.Length != 2) return;
//
//         if (currentText != null) currentText.text = parts[0].Trim();
//         if (reserveText != null) reserveText.text = "/" + parts[1].Trim();
//     }
// }