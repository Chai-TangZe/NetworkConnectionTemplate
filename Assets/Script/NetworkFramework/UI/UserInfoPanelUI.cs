using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 大厅「人物信息」面板：仅编辑账号级 <see cref="UserData"/>（Id、名字、描述、头像）。
/// </summary>
public class UserInfoPanelUI : MonoBehaviour
{
    [Header("根节点")]
    [SerializeField] GameObject panelRoot;

    [Header("只读展示")]
    [SerializeField] Text playerIdText;
    [SerializeField] Text nameDisplayText;
    [SerializeField] Text descDisplayText;
    [SerializeField] Image avatarDisplayImage;

    [Header("编辑")]
    [SerializeField] InputField nameEditInput;
    [SerializeField] InputField descEditInput;
    [SerializeField] Button avatarEditButton;
    [SerializeField] Image avatarEditImage;

    [Header("模式切换")]
    [SerializeField] GameObject viewBlocksRoot;
    [SerializeField] GameObject editBlocksRoot;
    [SerializeField] Button modifyOrSaveButton;
    [SerializeField] Text modifyOrSaveButtonLabel;

    [Header("头像选择")]
    [SerializeField] GameObject avatarPickerRoot;
    [SerializeField] Transform avatarGridParent;
    [SerializeField] AvatarPickerItem avatarCellPrefab;

    [Header("文案")]
    [SerializeField] string labelModify = "修改";
    [SerializeField] string labelSave = "保存";

    bool isEditMode;
    int pendingAvatarId;

    public event Action OnPanelClosed;

    void Awake()
    {
        if (avatarEditButton != null)
        {
            avatarEditButton.onClick.AddListener(OnClickAvatarToPick);
        }

        if (modifyOrSaveButton != null)
        {
            modifyOrSaveButton.onClick.AddListener(OnClickModifyOrSave);
        }
    }

    void OnEnable()
    {
        if (PlayerProfileContext.Instance != null)
        {
            PlayerProfileContext.Instance.ProfileChanged += OnProfileChanged;
        }
    }

    void OnDisable()
    {
        if (PlayerProfileContext.Instance != null)
        {
            PlayerProfileContext.Instance.ProfileChanged -= OnProfileChanged;
        }
    }

    void OnProfileChanged()
    {
        if (panelRoot != null && panelRoot.activeSelf && !isEditMode)
        {
            RefreshViewFromProfile();
        }
    }

    public void Open()
    {
        PlayerProfileContext context = PlayerProfileContext.Instance ?? PlayerProfileContext.EnsureInstance();
        context.EnsureDefaults();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        isEditMode = false;
        pendingAvatarId = context.User.AvatarId;
        ApplyModeVisuals();
        RefreshViewFromProfile();
        SyncEditFieldsFromProfile();
        CloseAvatarPicker();
    }

    public void Close()
    {
        isEditMode = false;
        CloseAvatarPicker();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        OnPanelClosed?.Invoke();
    }

    public void OnClickBack()
    {
        Close();
    }

    public void OnClickModifyOrSave()
    {
        PlayerProfileContext context = PlayerProfileContext.Instance;
        if (context == null)
        {
            return;
        }

        if (!isEditMode)
        {
            isEditMode = true;
            context.EnsureDefaults();
            pendingAvatarId = context.User.AvatarId;
            SyncEditFieldsFromProfile();
            ApplyAvatarEditPreview();
            ApplyModeVisuals();
            return;
        }

        SaveFromInputs();
    }

    void SaveFromInputs()
    {
        PlayerProfileContext context = PlayerProfileContext.Instance;
        if (context == null)
        {
            return;
        }

        context.EnsureDefaults();
        UserData user = context.User;

        string name = nameEditInput != null ? nameEditInput.text : user.DisplayName;
        string desc = descEditInput != null ? descEditInput.text : string.Empty;

        user.DisplayName = string.IsNullOrWhiteSpace(name) ? user.DisplayName : name.Trim();
        user.Description = desc ?? string.Empty;
        user.AvatarId = AvatarDataRepository.ResolveEffectiveAvatarId(pendingAvatarId);

        context.SaveProfile();
        TryPushProfileToRoomPlayer();

        isEditMode = false;
        ApplyModeVisuals();
        RefreshViewFromProfile();
    }

    void TryPushProfileToRoomPlayer()
    {
        RoomManager roomManager = RoomManager.Instance;
        if (roomManager == null)
        {
            return;
        }

        RoomPlayer local = roomManager.GetLocalRoomPlayer();
        if (local == null)
        {
            return;
        }

        PlayerProfileContext ctx = PlayerProfileContext.Instance;
        if (ctx == null)
        {
            return;
        }

        UserData user = ctx.User;
        if (user == null)
        {
            return;
        }

        local.CmdSetPlayerInfo(user.DisplayName, user.AvatarId, user.Description ?? string.Empty);
    }

    public void OnClickAvatarToPick()
    {
        if (!isEditMode)
        {
            return;
        }

        OpenAvatarPicker();
    }

    void OpenAvatarPicker()
    {
        if (avatarPickerRoot != null)
        {
            avatarPickerRoot.SetActive(true);
        }

        if (avatarGridParent == null || avatarCellPrefab == null)
        {
            return;
        }

        for (int i = avatarGridParent.childCount - 1; i >= 0; i--)
        {
            Destroy(avatarGridParent.GetChild(i).gameObject);
        }

        foreach (AvatarDataDefinition def in AvatarDataRepository.GetAll())
        {
            if (def == null)
            {
                continue;
            }

            AvatarPickerItem cell = Instantiate(avatarCellPrefab, avatarGridParent);
            cell.SetData(def, OnAvatarCellSelected);
        }
    }

    void OnAvatarCellSelected(int avatarId)
    {
        pendingAvatarId = avatarId;
        ApplyAvatarEditPreview();
        CloseAvatarPicker();
    }

    void CloseAvatarPicker()
    {
        if (avatarPickerRoot != null)
        {
            avatarPickerRoot.SetActive(false);
        }
    }

    void ApplyModeVisuals()
    {
        // 先展开编辑区与头像按钮，再收起查看区；否则「头像编辑按钮」若挂在 viewBlocksRoot 下会被一并隐藏，打包后表现为点了修改也不出按钮。
        if (editBlocksRoot != null)
        {
            editBlocksRoot.SetActive(isEditMode);
        }

        if (avatarEditButton != null)
        {
            avatarEditButton.gameObject.SetActive(isEditMode);
        }

        if (viewBlocksRoot != null)
        {
            viewBlocksRoot.SetActive(!isEditMode);
        }

        if (modifyOrSaveButtonLabel != null)
        {
            modifyOrSaveButtonLabel.text = isEditMode ? labelSave : labelModify;
        }

        if (avatarDisplayImage != null)
        {
            avatarDisplayImage.enabled = !isEditMode;
        }
    }

    void RefreshViewFromProfile()
    {
        PlayerProfileContext context = PlayerProfileContext.Instance;
        if (context == null)
        {
            return;
        }

        context.EnsureDefaults();
        UserData user = context.User;

        if (playerIdText != null)
        {
            playerIdText.text = string.IsNullOrWhiteSpace(user.UserId) ? "--" : user.UserId;
        }

        if (nameDisplayText != null)
        {
            nameDisplayText.text = user.DisplayName;
        }

        if (descDisplayText != null)
        {
            descDisplayText.text = string.IsNullOrWhiteSpace(user.Description) ? string.Empty : user.Description;
        }

        ApplySpriteToImage(avatarDisplayImage, AvatarDataRepository.GetIconOrNull(user.AvatarId));
    }

    void SyncEditFieldsFromProfile()
    {
        PlayerProfileContext context = PlayerProfileContext.Instance;
        if (context == null)
        {
            return;
        }

        context.EnsureDefaults();
        UserData user = context.User;

        if (nameEditInput != null)
        {
            nameEditInput.text = user.DisplayName;
        }

        if (descEditInput != null)
        {
            descEditInput.text = user.Description ?? string.Empty;
        }

        pendingAvatarId = user.AvatarId;
        ApplyAvatarEditPreview();
    }

    void ApplyAvatarEditPreview()
    {
        ApplySpriteToImage(avatarEditImage, AvatarDataRepository.GetIconOrNull(pendingAvatarId));
    }

    static void ApplySpriteToImage(Image image, Sprite icon)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = icon != null ? icon : UiPlaceholderSprite.White();
        image.enabled = true;
    }
}
