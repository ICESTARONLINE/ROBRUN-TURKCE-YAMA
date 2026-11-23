"""Apply the Nov 2026 string update onto the Turkish dictionaries and bundle."""
from __future__ import annotations

import json
import shutil
import sys
from pathlib import Path

import UnityPy

ROOT = Path(r"D:\robrun")
sys.path.insert(0, str(ROOT / "tools" / "tr_worker"))
from install_cache import (  # noqa: E402
    TABLE_PREFIXES,
    fold_builtin_text,
    uses_builtin_font,
)

NEW_EN = ROOT / "localization-string-tables-english(unitedstates)(en-us)_assets_all.bundle"
OLD_TR = (
    ROOT
    / "installer"
    / "PatchData"
    / "localization-string-tables-english(unitedstates)(en-us)_assets_all.bundle"
)
DATA = ROOT / "tools" / "tr_worker" / "data"
TR_NAME = "localization-string-tables-turkish(turkey-republic)(tr)_assets_all_turkish_.bundle"
OUTS = [
    DATA / TR_NAME,
    ROOT / "installer" / "PatchData" / TR_NAME,
    ROOT / TR_NAME,
]

# Existing keys whose English changed.
CHANGED = {
    "StaticStrings::skin_center_go_to_pack_button": "Teklifi Gör",
    "StaticStrings::skin_center_set_packs_timer_tooltip_title": "Öne Çıkan Teklif",
    "StaticStrings::skin_center_set_packs_timer_tooltip_description": "Süre dolunca mağazada başka bir teklif görünür.",
    "StaticStrings::hints_money_workbench_description": "<color=#{:palette(Second)}>Aklama Masası</color> büyük miktarda <color=#{:palette(Second)}>nakit</color> saklamanı sağlar. Güvenli bir banka hesabı açınca parayı aklayıp oraya aktarabilirsin. Masayı geliştirerek aklamayı hızlandırabilirsin; işlem kendiliğinden yürür. Hesabından nakit çekmek için <color=#{:palette(Second)}>ATM'leri</color> kullan.",
    "StaticStrings::baitobject_notification_execution": "Henüz infaz yok.",
    "StaticStrings::dev_plan_next_update_title": "Oyuna gelecekler",
    "StaticStrings::popup_confiscation_line4": "<color=#{:palette(Second)}>Highlands</color> sakinleri mülklerini <color=#{:palette(Second)}>koruyarak</color> yeni bir eyalete taşınabilir, ancak <color=#{:palette(Second)}>en azılı suçlularla</color> rekabet etmek zorunda kalırlar.",
    "StaticStrings::buy_settelment_dialog_description_text": "Arazi, tüm sakinlerin <color=#{:palette(Main)}>yerleşimine açılır</color>. Yerleşimin sahibi, <color=#{:palette(Main)}>orada aklanan tüm paranın ve tüm Karaborsa işlemlerinin belli bir yüzdesini</color> alır.",
    "StaticStrings::skin_center_buy_pack_button": "Satın Al {0}",
    "StaticStrings::skin_center_shop_pack_modal_description": "<color=#{:palette(Second)}>{0}</color> satın alınsın mı?",
    "StaticStrings::skin_center_shop_pack_modal_parts_description": "{0} içindeki bazı eşyalar koleksiyonunda zaten var. Karşılığında {1} alırsın.",
    "StaticStrings::mail_crypto_tooltip_description": "Kaplama, paket ve Battle Pass satın almak için kullanılan premium para birimi.",
    "StaticStrings::prison_accusation_dialog_sentence_limit_reached": "Toplam hapis cezası sınırına ulaşıldı - {0:plural:{} yıl|{} yıl|{} yıl}.",
    "StaticStrings::codex_buy_settlement_description": "<color=#{:palette(Second)}>Yerleşim Tapusu</color> bir yerleşim satın almanı sağlar. Tapuyu aldıktan sonra dünya haritasında uygun seviyedeki bir bölgede sahipsiz bir yerleşim bul ve belgeyi kullan. Sahiplendiğin yerleşim böylece iskâna açılır.\nSahibi olarak bir arsayı <color=#{:palette(Second)}>ücretsiz</color> ayırabilir, sakinler <color=#{:palette(Second)}>nakit akladığında</color> ya da <color=#{:palette(Second)}>Karaborsa'da iş yaptığında</color> onlardan <color=#{:palette(Second)}>vergi alabilirsin</color>.",
    "StaticStrings::skin_center_season_tab": "Pass",
    "StaticStrings::streamer_mode_hidden_label": "Gizli",
    "StaticStrings::battle_pass_currency_earned": "Kazanılan Battle Pass parası: {0}",
    "StaticStrings::future_next_title": "Oyuna gelecekler",
    "StaticStrings::battle_pass_1_title": "Saf Kimya",
    "StaticStrings::battle_pass_1_description": "Kimya hassasiyet ister. Sokaklar cesaret ister. Gerçek para ikisini birden ister.\n\nLooty Bay'de iş çevirip <color=#{:palette(Main)}> Battle Pass parası</color> biriktir ve <color=#{:palette(Main)}>ücretsiz</color> ödüllere dönüştür. Özel kozmetikleri kapmak için <color=#{:palette(Main)}>Premium</color> Battle Pass satın al.",
    "StaticStrings::battle_pass_premium_dialog_description": "<color=#{:palette(Main)}>Premium Battle Pass ödüllerinin</color> kilidini açar.",
    "StaticStrings::battle_pass_premium_plus_dialog_description": "<color=#{:palette(Main)}>Tüm Premium Battle Pass ödüllerinin</color> kilidini anında açar ve hepsini verir.",
    "StaticStrings::battle_pass_buy_premium_modal_description": "Premium Battle Pass ödüllerinin kilidi açılsın mı?",
    "StaticStrings::battle_pass_buy_premium_plus_modal_description": "Tüm Premium Battle Pass ödüllerinin kilidi anında açılsın mı?",
    "StaticStrings::battle_pass_not_enough_points_notification": "Yeterli Battle Pass parası yok",
    "StaticStrings::battle_pass_rewards_repeatable_tooltip_description": "Battle Pass parasıyla alınan her ödül rastgele bir envanter eşyası verir. Satın alma sınırı yoktur.",
    "StaticStrings::battle_pass_rewards_main_tooltip_description": "Ana Battle Pass ödülü, diğer tüm premium Battle Pass ödülleri alındıktan sonra açılır.",
    "StaticStrings::battle_pass_collect_all_skins_notification": "Önce tüm Premium Battle Pass ödüllerini al",
    "StaticStrings::battle_pass_new_season_title": "Yeni Battle Pass",
    "StaticStrings::battle_pass_leave_realm_points_lost": "Biriken Battle Pass paran kaybolacak: {0}",
    "StaticStrings::unavailable_in_hidden_object": "Gizlenmişken kullanılamaz",
    "StaticStrings::unavailable_in_death": "Ölünce kullanılamaz",
    "StaticStrings::steam_overlay_requirement": "Steam arayüzü (overlay) açılamadı. Etkinleştirip oyunu yeniden başlat.",
}

BY_KEY = {
    "skin_center_character_skin_masks_9_c3": 'Motorcu "Diş"',
    "skin_center_character_skin_masks_7_c1": "Solunum Maskesi",
    "skin_center_character_skin_masks_6_c2": "Alev Kaynakçısı",
    "skin_center_character_skin_masks_3_c2": "Karbon Balistik",
    "skin_center_character_skin_masks_3_c1": "Siyah Balistik",
    "skin_center_character_skin_masks_28_c1": "Kent Motorcusu",
    "skin_center_character_skin_masks_21_c1": "Yıldız Boynuz",
    "skin_center_character_skin_masks_19_c1": "Pandomim Bebek",
    "skin_center_character_skin_masks_18_c1": "Kafatası Solunum Maskesi",
    "skin_center_character_skin_masks_17_c1": "Palyaço",
    "skin_center_character_skin_masks_15_c1": "Altın Kafatası",
    "skin_center_character_skin_masks_13_c1": "Altın Bebek",
    "skin_center_character_skin_masks_12_1": "Maymun",
    "skin_center_character_skin_masks_11_c1": "İblis",
    "skin_center_character_skin_masks_16_c2": "Yeşil Âşık",
    "skin_center_character_skin_masks_31_c1": "Vahşi Batı",
    "skin_center_character_skin_masks_33_c1": "Panter",
    "skin_center_character_skin_masks_36_c1": "Yelpaze",
    "skin_center_character_skin_m_body_19_c2": "Özgürlük Selamı",
    "skin_center_character_skin_m_body_19_c1": "Gölge Özgürlük",
    "skin_center_character_skin_f_body_3_t2_c3": "AKINA Koşucusu",
    "skin_center_character_skin_f_body_3_t2_c2": "GYM Koşucusu",
    "skin_center_character_skin_f_body_3_t2_c1": "HUNTER Koşucusu",
    "skin_center_character_skin_f_body_19_c3": "Fırtına Özgürlük",
    "skin_center_character_skin_f_body_19_c1": "Gölge Özgürlük",
    "skin_center_character_skin_m_body_3_c2": "AKINA Sportif",
    "skin_center_character_skin_m_body_3_c3": "Kurt Sportif",
    "skin_center_character_skin_m_body_3_c4": "Enerji Sportif",
    "skin_center_character_skin_m_body_24_c1": "Çiftçi Köyü",
    "skin_center_character_skin_m_body_24_c2": "Avcı Köyü",
    "skin_center_character_skin_m_body_24_c3": "Oduncu Köyü",
    "skin_center_character_skin_f_body_35_c1": "Baskın",
    "skin_center_character_skin_f_body_22_c2": "SPARTA Sporcusu",
    "skin_center_character_skin_f_body_24_c1": "Çiftçi Köyü",
    "skin_center_character_skin_f_body_24_c2": "Avcı Köyü",
    "skin_center_character_skin_f_body_24_c3": "Oduncu Köyü",
    "skin_center_character_skin_m_body_46_t1_c1": "Ajan",
    "skin_center_character_skin_bags_9_c1": "Timsah Premium",
    "skin_center_character_skin_bags_14_c1": "Siyah Kent",
    "skin_center_character_skin_bags_13_c1": "Kızıl İblis",
    "skin_center_character_skin_bags_12_c1": "Kent Leydisi",
    "skin_center_character_skin_bags_13_c2": "Kahverengi İblis",
    "skin_center_character_skin_bags_18_c1": "Bağımsızlık",
    "skin_center_character_skin_bags_18_c2": "Kızıl Tepe",
    "skin_center_character_skin_bags_23_c1": "Leopar",
    "skin_center_character_skin_bags_26_c1": "Klain",
    "skin_center_character_skin_keychain_42_c1": "Mermi",
    "car_longhorn_titan_skin_name_6": "Devriye Tuning",
    "car_fukurou_skin_spark_name_6": "Akina Tuning",
    "car_royal_j_skin_name_8": "Parlak Tuning",
    "car_buletti_soltempra_skin_name_8": "Beyaz Corsa Tuning",
    "car_longhorn_van_skin_name_9": "Kimyager Tuning",
    "skin_center_weapon_skin_jp_7_s_1": "Koşu Boyası",
    "skin_center_weapon_skin_jp_7_s_5": "Odak Boyası",
    "skin_center_weapon_skin_jp_7_s_4": "Tatlı Boyası",
    "skin_center_weapon_skin_cb_45_s_1": "Senfoni Boyası",
    "skin_center_weapon_skin_pdw_90_s_2": "Gizlilik Boyası",
    "skin_center_weapon_skin_pdw_90_s_5": "Mineral Boyası",
    "skin_center_weapon_skin_pdw_90_s_7": "Serap Boyası",
    "skin_center_weapon_skin_ap_scalar_s_1": "Avcı Boyası",
    "skin_center_weapon_skin_ap_scalar_s_2": "Çağ Boyası",
    "skin_center_weapon_skin_dp_9_s_2": "Alev Boyası",
    "skin_center_weapon_skin_lbr_44_s_3": "Vahşi Batı Boyası",
    "skin_center_weapon_skin_kl_545_s_2": "Tecrübe Boyası",
    "skin_center_weapon_skin_rc_556_s_1": "Yıldızlar ve Şeritler Boyası",
    "skin_center_weapon_skin_rc_556_s_2": "Gölge Boyası",
    "skin_center_weapon_skin_rc_556_s_5": "Ok Boyası",
    "skin_center_weapon_skin_drm_14_s_4": "Kurt İni Boyası",
    "skin_center_weapon_skin_drm_14_s_6": "Garaj Boyası",
    "skin_center_weapon_skin_harris_500_s_1": "Yargıç Boyası",
    "skin_center_weapon_skin_harris_500_s_2": "Yıldırım Boyası",
    "skin_center_weapon_skin_harris_500_s_5": "Polaris Boyası",
    "skin_center_weapon_skin_tag_12k_s_6": "Ajan Boyası",
}

BY_ID = {
    "550091092536590336": "<color=#{:palette(Attention)}>Yeniden paketlemek için ev bölgesine yerleştirilmiş bir Kaçak Mal Tezgâhı gerekir</color>.",
    "547673623673954304": "Bölge değiştiriliyor. Lütfen bekle...",
    "549750143738445824": "Günlük hapis cezası sınırına ulaşıldı - {0:plural:{} yıl|{} yıl|{} yıl}.",
    "549800093776084992": "Sohbet, en az {0} yıl hapis cezası ya da bir arsa veya yerleşim sahipliğiyle açılır.",
    "549800093780279296": "Sınırlı İletişim",
    "549800093784473600": "Parti ve klan sohbeti herkese açık. Sesli sohbet, Lenny’s Motel ve hapishane dışında her yerde kullanılabilir. Tüm kanallarda ve konumlarda kısıtlamasız sohbet için en az {0} yıl hapis cezası ya da bir arsa veya yerleşim sahipliği gerekir.",
    "549800093788667904": "Sohbet, en az {0} yıl hapis cezası ya da bir arsa veya yerleşim sahipliğiyle açılır.",
    "550826256547393536": "...sunucuyla eşitleniyor",
    "549794597823307776": "Sunucu:",
    "549908523768602624": "Ichigeki Boyası",
    "549910824570839040": "Rensha Boyası",
    "549911980802039808": "Limit Boyası",
    "549912417093541888": "Patiler Boyası",
    "549916478228389888": "Pembe Motorcu",
    "549926184594432000": "İş Boyası",
    "549928484612333568": "Eşik Boyası",
    "558965646124713532": "Hesaba para yatır",
    "559829690545038156": "Hesabına yalnızca evindeki Aklama Masası ile para yatırabilirsin.",
    "748100000000000101": "KÖTÜ TAYFA",
    "748100000000000102": "İlerlemeye devam et. Bir sonraki büyük vurgun için sokak tarzı ve ağır tekerlekler.",
    "748100000000000103": "AJAN",
    "748100000000000104": "Soğuk hesap. İsabetli atışlar. Şansa yer yok.",
    "748100000000000105": "KIZIL TEPE",
    "748100000000000106": "Ufkun ötesinde özgürlük. Tozlu patikalar, güvenilir bir altıpatlar ve kendi kurallarınla bir hayat.",
    "748100000000000107": "Yeni bir görünüm. Teklif yenilenene kadar geçerli.",
    "748100000000000108": "ALPHA",
    "748100000000000109": "Temiz hatlar. Keskin tarz. Her ayrıntı yerli yerinde.",
    "748100000000000110": "DRIFT",
    "748100000000000111": "Her köşede iz bırak. Karanlıktan sonraki şehir için sokak tarzı.",
    "550094259027742720": "Maske özelleştirme",
    "550094259073880064": "Araç özelleştirme",
    "550445367170162688": "Küçük Pati",
    "550448778070450176": "ALPHA Sporcusu",
    "550450803415965696": "ALPHA Sporcusu",
    "559829942201371024": "Mevcut bölgedeki arkadaşlar <uppercase>{0}{1:<0?|=0?-|{1}}</uppercase>",
    "748100000000000112": "DOKUZ CAN",
    "748100000000000113": "Pembenin tonlarında rahat bir görünüm.",
    "550507869006454784": "Nya Boyası",
    "550508113593098240": "Tüylü Sokak",
    "550578623001649152": "GELİŞTİRME AŞAMASINDA!\n<color=#{:palette(Second)}>Highlands</color> sakinleri mülklerini <color=#{:palette(Second)}>koruyarak</color> yeni bir eyalete taşınabilir, ancak <color=#{:palette(Second)}>en azılı suçlularla</color> rekabet etmek zorunda kalırlar.",
    "547592892499091456": "Sunucu: {2:<0?{0}|=0?-|{1} {2}}",
    "550136619493265408": "Yalnızca arkadaşların olduğu sunucuları göster",
    "550136619493265410": "Sunucudaki arkadaşlar",
    "550136619493265411": "Çevrimiçi",
    "550136619493265412": "Ve {0} kişi daha",
    "550136619493265413": "Çevrimiçi arkadaşlar",
    "550552809049108480": "Başka bir cihazda oturum açtın.",
}


def dump_dict(path: Path, data: dict) -> None:
    path.write_text(
        json.dumps(data, ensure_ascii=False, indent=0) + "\n",
        encoding="utf-8",
    )


def main() -> None:
    tr_path = DATA / "robrun_tr.json"
    tr = json.loads(tr_path.read_text(encoding="utf-8"))
    missing_changed = [k for k in CHANGED if k not in tr]
    if missing_changed:
        raise SystemExit("missing changed keys: " + ", ".join(missing_changed))
    tr.update(CHANGED)

    shared = json.loads((DATA / "shared_keys.json").read_text(encoding="utf-8"))
    builtin = set(json.loads((DATA / "builtin_keys.json").read_text(encoding="utf-8")))
    extra_path = DATA / "extra_tr.json"
    extra = json.loads(extra_path.read_text(encoding="utf-8"))

    old_env = UnityPy.load(str(OLD_TR))
    old_tr: dict[str, str] = {}
    for obj in old_env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        tree = obj.read_typetree()
        for entry in tree.get("m_TableData") or []:
            old_tr[str(entry.get("m_Id"))] = entry.get("m_Localized") or ""

    env = UnityPy.load(str(NEW_EN))
    applied = kept = 0
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        tree = obj.read_typetree()
        name = tree.get("m_Name") or ""
        prefix = next((p for p in TABLE_PREFIXES if name.startswith(p)), None)
        if not prefix:
            continue
        id_to_key = shared.get(prefix, {})
        for entry in tree.get("m_TableData") or []:
            eid = str(entry.get("m_Id"))
            key = id_to_key.get(eid)
            en = entry.get("m_Localized") or ""
            lkey = f"{prefix}::{key}" if key else None
            text = BY_ID.get(eid)
            if text is None and key:
                text = BY_KEY.get(key)
            if text is None and lkey in CHANGED:
                text = CHANGED[lkey]
            if text is None and eid in old_tr:
                text = old_tr[eid]
                kept += 1
            if text is None and lkey:
                text = tr.get(lkey)
            if text is None and en in extra:
                text = extra[en]
            if text is None:
                continue
            if entry.get("m_Localized") != text:
                entry["m_Localized"] = text
                applied += 1
            if eid in BY_ID or (key and key in BY_KEY):
                extra[en] = BY_ID.get(eid) or BY_KEY[key]
            if key and key in BY_KEY:
                tr[f"{prefix}::{key}"] = BY_KEY[key]
            elif eid in BY_ID:
                tr[f"{prefix}::id_{eid}"] = BY_ID[eid]
        obj.save_typetree(tree)

    raw = env.file.save(packer="lz4")
    for dest in OUTS:
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(raw)

    dump_dict(tr_path, tr)
    root_extra = ROOT / "tools" / "tr_worker" / "extra_tr.json"
    text = json.dumps(extra, ensure_ascii=False, indent=2) + "\n"
    extra_path.write_text(text, encoding="utf-8")
    root_extra.write_text(text, encoding="utf-8")
    print(f"applied_replacements={applied} kept_old={kept} bundle={len(raw)}")
    print(f"new_keys={len(BY_KEY)} new_ids={len(BY_ID)} changed={len(CHANGED)}")


if __name__ == "__main__":
    main()
