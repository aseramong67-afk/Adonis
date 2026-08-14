(() => {
  "use strict";

  const $ = (sel) => document.querySelector(sel);

  const THEME_KEY = "adonis-theme";

  function applyTheme(light) {
    document.documentElement.setAttribute("data-theme", light ? "light" : "dark");
    const toggle = $("#themeLight");
    if (toggle) toggle.checked = !!light;
    try { localStorage.setItem(THEME_KEY, light ? "light" : "dark"); } catch {}
  }

  function loadTheme() {
    let light = false;
    try { light = localStorage.getItem(THEME_KEY) === "light"; } catch {}
    applyTheme(light);
  }
  const grid = $("#addons");
  const searchInput = $("#search");
  const targetPathEl = $("#targetPath");
  const statsEl = $("#stats");
  const loadingEl = $("#loading");
  const emptyEl = $("#empty");

  let catalogFilter = "addons";

  const DEFAULT_ACCENT = "#ff9f1c";

  const PALETTE = [
    { hex: "#ff9f1c", label: "Янтарный" },
    { hex: "#4ade80", label: "Зелёный" },
    { hex: "#38bdf8", label: "Голубой" },
    { hex: "#22d3ee", label: "Бирюзовый" },
    { hex: "#a78bfa", label: "Фиолетовый" },
    { hex: "#f472b6", label: "Розовый" },
    { hex: "#f87171", label: "Красный" },
    { hex: "#e5e7eb", label: "Светлый" }
  ];

  const icons = {
    check: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6L9 17l-5-5"/></svg>',
    x: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>',
    info: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>',
    folder: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>',
    install: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>',
    trash: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>',
    logout: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>',
    discord: '<svg viewBox="0 0 24 24" fill="currentColor"><path d="M20.32 4.37a19.8 19.8 0 0 0-4.93-1.51 13.8 13.8 0 0 0-.64 1.28 18.3 18.3 0 0 0-5.5 0 13.8 13.8 0 0 0-.64-1.28 19.7 19.7 0 0 0-4.93 1.51C.53 9.05-.32 13.6.1 18.06a19.9 19.9 0 0 0 6.07 3.06c.49-.66.92-1.37 1.3-2.1a12.9 12.9 0 0 1-2.05-.98c.17-.13.34-.26.5-.39a14.1 14.1 0 0 0 12.16 0c.16.13.33.26.5.39a12.9 12.9 0 0 1-2.05.98c.38.73.81 1.44 1.3 2.1a19.8 19.8 0 0 0 6.07-3.06c.5-5.16-.84-9.66-3.58-13.69zM8.02 15.33c-1.18 0-2.16-1.08-2.16-2.42 0-1.33.96-2.42 2.16-2.42 1.21 0 2.18 1.1 2.16 2.42 0 1.34-.96 2.42-2.16 2.42zm7.96 0c-1.18 0-2.16-1.08-2.16-2.42 0-1.33.96-2.42 2.16-2.42 1.21 0 2.18 1.1 2.16 2.42 0 1.34-.95 2.42-2.16 2.42z"/></svg>',
    user: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>',
    reset: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 1 0 2.64-6.36L3 8"/><path d="M3 3v5h5"/></svg>',
    steam: '<svg viewBox="0 0 24 24" fill="currentColor"><path d="M11.979 0C5.678 0 .511 4.86.022 11.037l6.432 2.658c.545-.371 1.203-.59 1.912-.59.063 0 .125.004.188.006l2.861-4.142V8.91c0-2.495 2.028-4.524 4.524-4.524 2.494 0 4.524 2.031 4.524 4.527s-2.03 4.525-4.524 4.525h-.105l-4.076 2.911c0 .052.004.105.004.159 0 1.875-1.515 3.396-3.39 3.396-1.635 0-3.016-1.173-3.331-2.727L.436 15.27C1.862 20.307 6.486 24 11.979 24c6.627 0 12-5.373 12-12S18.605 0 11.979 0zM7.54 18.21l-1.473-.61c.262.543.714.999 1.314 1.25 1.297.539 2.793-.076 3.332-1.375.263-.63.264-1.319.005-1.949s-.75-1.121-1.377-1.383c-.624-.26-1.29-.249-1.878-.03l1.523.63c.956.4 1.409 1.5 1.009 2.455-.397.957-1.497 1.41-2.454 1.012H7.54zm11.415-9.303c0-1.662-1.353-3.015-3.015-3.015-1.665 0-3.015 1.353-3.015 3.015 0 1.665 1.35 3.015 3.015 3.015 1.663 0 3.015-1.35 3.015-3.015zm-5.273-.005c0-1.252 1.013-2.266 2.265-2.266 1.249 0 2.266 1.014 2.266 2.266 0 1.251-1.017 2.265-2.266 2.265-1.253 0-2.265-1.014-2.265-2.265z"/></svg>',
    star: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>',
    starFilled: '<svg viewBox="0 0 24 24" fill="currentColor"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>',
    ok: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 12l2 2 4-4"/><circle cx="12" cy="12" r="9"/></svg>',
    err: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><line x1="9" y1="9" x2="15" y2="15"/><line x1="15" y1="9" x2="9" y2="15"/></svg>'
  };

  function toast(message, type = "info") {
    const el = document.createElement("div");
    el.className = `toast ${type}`;
    el.innerHTML = `${icons[type] || icons.info}<span></span>`;
    el.querySelector("span").textContent = message;
    $("#toasts").appendChild(el);
    setTimeout(() => {
      el.classList.add("out");
      el.addEventListener("animationend", () => el.remove());
    }, 3200);
  }

  function escapeHtml(str) {
    return String(str ?? "").replace(/[&<>"']/g, (c) => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));
  }

  // ---------- window ----------

  function getBridge() {
    return window.chrome?.webview?.hostObjects?.adonisBridge || null;
  }

  $("#titlebar").addEventListener("mousedown", (e) => {
    if (e.button !== 0 || e.target.closest("button")) return;
    const bridge = getBridge();
    if (bridge) bridge.BeginDrag();
  });

  function bindWinButton(id, bridgeFn) {
    const btn = document.getElementById(id);
    if (btn) btn.addEventListener("click", () => {
      const bridge = getBridge();
      if (bridge) bridgeFn(bridge);
    });
  }

  bindWinButton("winMin", (b) => b.Minimize());
  bindWinButton("winMax", (b) => b.ToggleMaximize());
  bindWinButton("winClose", (b) => b.Close());

  document.querySelectorAll(".resize-handle").forEach((h) => {
    h.addEventListener("mousedown", (e) => {
      if (e.button !== 0) return;
      e.preventDefault();
      const bridge = getBridge();
      if (bridge) bridge.BeginResize(parseInt(h.dataset.edge, 10));
    });
  });

  // ---------- views ----------

  function switchView(name) {
    const isCatalog = name === "addons" || name === "reskins";
    $("#view-addons").classList.toggle("hidden", !isCatalog);
    $("#view-settings").classList.toggle("hidden", name !== "settings");
    $("#view-binds").classList.toggle("hidden", name !== "binds");
    document.querySelectorAll(".nav-item").forEach((n) =>
      n.classList.toggle("active", n.dataset.view === name));
    $("#btnSettings")?.classList.toggle("active", name === "settings");

    if (isCatalog) {
      catalogFilter = name;
      loadAddons();
    }
    if (name === "settings") {
      loadSettings();
      loadAuth();
    }
    if (name === "binds") {
      loadBinds();
    }
  }

  document.querySelectorAll(".nav-item").forEach((n) =>
    n.addEventListener("click", () => switchView(n.dataset.view)));

  $("#btnSettings").addEventListener("click", () => switchView("settings"));

  document.querySelectorAll("#view-binds .subnav-item").forEach((btn) =>
    btn.addEventListener("click", () => {
      document.querySelectorAll("#view-binds .subnav-item").forEach((x) =>
        x.classList.toggle("active", x === btn));
      const show = btn.dataset.subview;
      $("#sub-binds").classList.toggle("hidden", show !== "binds");
      $("#sub-game").classList.toggle("hidden", show !== "game");
      if (show === "binds") loadBinds();
      if (show === "game") loadOptimization();
    }));

  // ---------- addons ----------

  function renderCard(addon) {
    const card = document.createElement("article");
    card.className = "card" + (addon.isInstalled ? " installed" : "");
    card.dataset.id = addon.id;
    card.dataset.title = (addon.title || "").toLowerCase();
    card.dataset.tags = (addon.tags || []).join(" ").toLowerCase();

    const preview = addon.previewImageUrl
      ? `<img src="${escapeHtml(addon.previewImageUrl)}" alt="" loading="lazy"
            onerror="this.parentElement.innerHTML='<span class=&quot;fallback&quot;>${icons.folder.replace(/"/g, "&quot;")}</span>'">`
      : `<span class="fallback">${icons.folder}</span>`;

    const typeBadge = addon.type ? `<span class="badge">${escapeHtml(addon.type)}</span>` : "";
    const installed = addon.isInstalled ? `<span class="installed-flag">Установлен</span>` : "";

    const authorHtml = addon.author
      ? `<div class="card-author" title="${escapeHtml(addon.author)}">
          ${addon.authorAvatar
            ? `<img class="author-avatar" src="${escapeHtml(addon.authorAvatar)}" alt="" loading="lazy">`
            : `<span class="author-avatar fallback">${escapeHtml((addon.author || "?").charAt(0).toUpperCase())}</span>`}
          <span class="author-name">${escapeHtml(addon.author)}</span>
        </div>`
      : "";

    const tags = (addon.tags || []).length
      ? `<div class="tags">${addon.tags.map((t) => `<span class="tag">${escapeHtml(t)}</span>`).join("")}</div>`
      : "";

    const btnClass = addon.isInstalled ? "btn danger" : "btn primary";
    const btnIcon = addon.isInstalled ? icons.trash : icons.install;
    const btnLabel = addon.isInstalled ? "Удалить" : "Установить";
    const btnAction = addon.isInstalled ? "uninstall" : "install";

    card.innerHTML = `
      <div class="card-preview">${preview}${typeBadge}${installed}</div>
      <div class="card-body">
        ${authorHtml}
        <h3>${escapeHtml(addon.title)}</h3>
        <p class="desc">${escapeHtml(addon.description) || "Без описания"}</p>
        ${tags}
      </div>
      <div class="card-foot">
        <span class="meta">
          <span class="added">Добавлен ${escapeHtml(addon.addedAtText)}</span>
          <span class="size">${escapeHtml(addon.sizeText)}</span>
        </span>
        <div class="card-actions">
          ${addon.workshopUrl ? `<button class="btn workshop" type="button" title="Открыть в Steam Workshop" data-workshop="${escapeHtml(addon.workshopUrl)}">${icons.steam}</button>` : ""}
          <button class="${btnClass}" data-action="${btnAction}" data-id="${escapeHtml(addon.id)}">${btnIcon}${btnLabel}</button>
        </div>
      </div>`;
    return card;
  }

  async function loadAddons() {
    loadingEl.classList.remove("hidden");
    grid.classList.add("hidden");
    emptyEl.classList.add("hidden");

    try {
      const res = await fetch("/api/addons");
      let addons = await res.json();
      const isReskin = (a) => (a.type || "").toLowerCase() === "reskin";
      if (catalogFilter === "reskins") addons = addons.filter(isReskin);
      else addons = addons.filter((a) => !isReskin(a));
      grid.innerHTML = "";
      for (const a of addons) grid.appendChild(renderCard(a));
      applyFilter();
      const installed = addons.filter((a) => a.isInstalled).length;
      statsEl.textContent = `${addons.length} аддонов · установлено ${installed}`;
    } catch {
      toast("Не удалось загрузить список аддонов", "err");
    } finally {
      loadingEl.classList.add("hidden");
      grid.classList.remove("hidden");
    }
  }

  function applyFilter() {
    const q = searchInput.value.trim().toLowerCase();
    let visible = 0;
    grid.querySelectorAll(".card").forEach((card) => {
      const match = !q || card.dataset.title.includes(q) || card.dataset.tags.includes(q);
      card.classList.toggle("hidden", !match);
      if (match) visible++;
    });
    emptyEl.classList.toggle("hidden", visible !== 0);
  }

  searchInput.addEventListener("input", applyFilter);

  grid.addEventListener("click", (e) => {
    const prev = e.target.closest(".card-preview img");
    if (prev) {
      openLightbox(prev.src);
      return;
    }
    const ws = e.target.closest("[data-workshop]");
    if (ws) {
      openWorkshop(ws.dataset.workshop);
      return;
    }
    const btn = e.target.closest("[data-action]");
    if (!btn) return;
    act(btn.dataset.id, btn.dataset.action);
  });

  function openWorkshop(url) {
    const bridge = getBridge();
    if (bridge) bridge.OpenBrowser(url);
    else window.open(url, "_blank");
  }

  $("#btnLaunchGmod").addEventListener("click", () => openWorkshop("steam://rungameid/4000"));

  // ---------- lightbox ----------

  const lightbox = $("#lightbox");
  const lightboxImg = lightbox.querySelector("img");

  function openLightbox(src) {
    lightboxImg.src = src;
    lightbox.classList.remove("hidden");
  }

  function closeLightbox() {
    lightbox.classList.add("hidden");
    lightboxImg.src = "";
  }

  lightbox.addEventListener("click", (e) => {
    if (e.target === lightbox) closeLightbox();
  });

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") closeLightbox();
  });

  // ---------- binds ----------

  let bindsList = [];
  let originalBinds = [];
  let catFilter = "Все";
  let statusFilter = "Все";
  let favFilter = "Все";

  const PINNED_BINDS = ["Купить здоровье", "Купить броню"];

  function rankBind(b) {
    const pi = PINNED_BINDS.indexOf((b.description || "").trim());
    if (pi !== -1) return pi;
    return b.favorite ? 100 : 200;
  }

  function sortBinds() {
    const ranked = bindsList.map((b, i) => ({ i, b }));
    ranked.sort((x, y) => rankBind(x.b) - rankBind(y.b) || x.i - y.i);
    bindsList = ranked.map((x) => x.b);
  }

  const CAT_CLASS = {
    "Админ": "c-admin", "Магазин": "c-magazin", "Анимации": "c-animacii", "Чат / РП": "c-chat", "Профессии": "c-professii", "Оружие": "c-oruzhie"
  };

  function updateBindsEmpty(shown) {
    $("#bindsEmptyText").textContent = bindsList.length === 0
      ? "Нет биндов. Нажмите «Конструктор»."
      : "Ничего не найдено по фильтру.";
    $("#bindsEmpty").classList.toggle("hidden", shown);
  }

  function bindSnapshot(b) {
    return { ...b };
  }

  function fillDropdown(menu, options, current, onPick) {
    menu.innerHTML = "";
    for (const [value, label] of options) {
      const item = document.createElement("button");
      item.type = "button";
      item.className = "dd-item" + (value === current ? " active" : "");
      item.textContent = label;
      item.addEventListener("click", () => {
        onPick(value, label);
        closeDropdowns();
      });
      menu.appendChild(item);
    }
  }

  function closeDropdowns() {
    document.querySelectorAll(".dd-menu").forEach((m) => m.classList.add("hidden"));
    document.querySelectorAll(".dd-btn").forEach((b) => b.classList.remove("open"));
  }

  function renderFilters() {
    const cats = [...new Set(bindsList.map((b) => (b.category || "").trim()).filter(Boolean))].sort();
    if (!["Все", ...cats].includes(catFilter)) catFilter = "Все";

    fillDropdown($("#ddCatMenu"), [["Все", "Категория: все"], ...cats.map((c) => [c, c])], catFilter, (v) => {
      catFilter = v;
      renderBinds();
    });
    $("#ddCatValue").textContent = catFilter === "Все" ? "Категория: все" : catFilter;

    const statusOpts = [["Все", "Статус: любые"], ["on", "Включённые"], ["off", "Выключенные"]];
    fillDropdown($("#ddStatusMenu"), statusOpts, statusFilter, (v) => {
      statusFilter = v;
      renderBinds();
    });
    $("#ddStatusValue").textContent = statusOpts.find(([k]) => k === statusFilter)[1];

    fillDropdown($("#ddFavMenu"), [["Все", "Избранные: любые"], ["fav", "Только избранные"]], favFilter, (v) => {
      favFilter = v;
      renderBinds();
    });
    $("#ddFavValue").textContent = favFilter === "Все" ? "Избранные: любые" : "Только избранные";
  }

  function bindMatches(b) {
    const catOk = catFilter === "Все"
      ? (b.category || "").trim() !== "Админ"
      : (b.category || "").trim() === catFilter;
    const statusOk = statusFilter === "Все"
      || (statusFilter === "on" && b.enabled)
      || (statusFilter === "off" && !b.enabled);
    const favOk = favFilter === "Все" || (favFilter === "fav" && b.favorite);
    return catOk && statusOk && favOk;
  }

  function renderBinds() {
    sortBinds();
    renderFilters();
    const container = $("#binds");    container.innerHTML = "";
    let shown = 0;
    bindsList.forEach((b, i) => {
      if (!bindMatches(b)) return;
      shown++;
      const isPlaceholderKey = b.key.trim().toLowerCase() === "кнопка";
      const hasOriginal = originalBinds.some((o) => o.id === b.id);
      const card = document.createElement("div");
      card.className = "bind-card " + (b.enabled ? "enabled" : "disabled");
      card.innerHTML = `
        <span class="keycap${isPlaceholderKey ? " placeholder" : ""}" title="${isPlaceholderKey ? "Замените «кнопка» на клавишу" : "Клавиша"}">
          <input class="bind-key" value="${escapeHtml(b.key)}" placeholder="F3" maxlength="1" spellcheck="false">
        </span>
        <div class="bind-main">
          <input class="bind-desc" value="${escapeHtml(b.description || "")}" placeholder="Что делает бинд" spellcheck="false">
          <div class="bind-code">
            <span class="bind-code-arrow">&gt;</span>
            <input class="bind-command" value="${escapeHtml(b.command)}" placeholder="say !kills" spellcheck="false">
          </div>
          <div class="bind-tags">
            ${b.category ? `<span class="bind-cat ${CAT_CLASS[b.category.trim()] || ""}">${escapeHtml(b.category)}</span>` : ""}
          </div>
        </div>
        <div class="bind-actions">
          <label class="switch" title="Включить/выключить">
            <input type="checkbox" class="bind-enabled" ${b.enabled ? "checked" : ""}>
            <span></span>
          </label>
          <div class="bind-btns">
            <button class="icon-btn bind-fav${b.favorite ? " active" : ""}" type="button" title="${b.favorite ? "Убрать из избранного" : "В избранное"}">${b.favorite ? icons.starFilled : icons.star}</button>
            ${hasOriginal ? `<button class="icon-btn bind-reset" type="button" title="Сбросить бинд">${icons.reset}</button>` : ""}
            <button class="icon-btn bind-del" type="button" title="Удалить">${icons.trash}</button>
          </div>
        </div>`;
      card.querySelector(".bind-key").addEventListener("input", (e) => { bindsList[i].key = e.target.value; });
      card.querySelector(".bind-desc").addEventListener("input", (e) => { bindsList[i].description = e.target.value; });
      card.querySelector(".bind-command").addEventListener("input", (e) => { bindsList[i].command = e.target.value; });
      card.querySelector(".bind-enabled").addEventListener("change", (e) => {
        bindsList[i].enabled = e.target.checked;
        card.classList.toggle("disabled", !e.target.checked);
        card.classList.toggle("enabled", e.target.checked);
      });
      const resetBtn = card.querySelector(".bind-reset");
      if (resetBtn) resetBtn.addEventListener("click", () => {
        const orig = originalBinds.find((o) => o.id === b.id);
        if (!orig) return;
        bindsList[i] = { ...bindSnapshot(orig) };
        renderFilters();
        renderBinds();
        toast("Бинд сброшен к исходному состоянию", "ok");
      });
      card.querySelector(".bind-del").addEventListener("click", () => { bindsList.splice(i, 1); renderFilters(); renderBinds(); });
      card.querySelector(".bind-fav").addEventListener("click", () => {
        bindsList[i].favorite = !bindsList[i].favorite;
        renderFilters();
        renderBinds();
      });
      container.appendChild(card);
    });
    $("#filterCount").textContent = bindsList.length ? `${shown} из ${bindsList.length}` : "";
    updateBindsEmpty(shown);
  }

  async function saveBinds() {
    const valid = bindsList.filter((b) => b.key.trim() && b.command.trim());
    if (!valid.length && bindsList.length) {
      toast("Заполните кнопку и команду", "err");
      return;
    }
    const btn = $("#btnSaveBinds");
    btn.disabled = true;
    try {
      const res = await fetch("/api/binds", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(valid)
      });
      const data = await res.json();
      if (data.ok) {
        bindsList = valid.map((b, idx) => ({ ...b, id: b.id || (originalBinds[idx]?.id) || idx + 1 }));
        originalBinds = bindsList.map(bindSnapshot);
        renderFilters();
        renderBinds();
        toast("Бинды сохранены и применены", "ok");
      } else {
        toast(data.message || "Ошибка сохранения", "err");
      }
    } catch {
      toast("Ошибка сохранения биндов", "err");
    } finally {
      btn.disabled = false;
    }
  }

  async function loadBinds() {
    try {
      const res = await fetch("/api/binds");
      const data = await res.json();
      bindsList = Array.isArray(data.binds)
        ? data.binds.map((b, idx) => ({
            id: idx + 1,
            key: b.key || "",
            command: b.command || "",
            description: b.description || "",
            category: b.category || "",
            enabled: b.enabled !== false,
            favorite: b.favorite === true
          }))
        : [];
      originalBinds = bindsList.map(bindSnapshot);
      renderFilters();
      renderBinds();
      syncFovSlider();
    } catch {
      toast("Не удалось загрузить бинды", "err");
    }
  }

  // ---------- viewmodel slider (удаление рук) ----------

  const fovRange = $("#fovRange");
  const fovValue = $("#fovValue");
  const handsToggle = $("#handsToggle");
  const fovCtl = $("#fovCtl");

  function findHandsBind() {
    return bindsList.find((x) => (x.description || "").trim() === "Убрать руки");
  }

  function syncFovSlider() {
    const b = findHandsBind();
    const m = b && b.command.match(/viewmodel_fov\s+(\d+)/i);
    const val = m ? Math.min(90, Math.max(54, +m[1])) : 90;
    const on = !!b && b.enabled !== false;
    fovRange.value = val;
    fovValue.textContent = val;
    handsToggle.checked = on;
    fovCtl.classList.toggle("off", !on);
  }

  function setFovBind() {
    const val = fovRange.value;
    fovValue.textContent = val;
    if (!handsToggle.checked) return;
    const idx = bindsList.findIndex((b) => (b.description || "").trim() === "Убрать руки");
    if (idx !== -1) {
      bindsList[idx].command = `viewmodel_fov ${val}`;
      bindsList[idx].enabled = true;
    } else {
      bindsList.push({
        id: originalBinds.length + bindsList.length + 1,
        key: "кнопка",
        command: `viewmodel_fov ${val}`,
        description: "Убрать руки",
        category: "Разное",
        author: currentUserName || "Гость",
        enabled: true,
        favorite: false
      });
    }
    renderFilters();
    renderBinds();
  }

  function toggleHands() {
    const on = handsToggle.checked;
    fovCtl.classList.toggle("off", !on);
    const idx = bindsList.findIndex((b) => (b.description || "").trim() === "Убрать руки");
    if (on) {
      if (idx === -1) {
        setFovBind();
      } else {
        bindsList[idx].enabled = true;
        renderFilters();
        renderBinds();
      }
    } else if (idx !== -1) {
      bindsList[idx].enabled = false;
      renderFilters();
      renderBinds();
    }
  }

  fovRange.addEventListener("input", setFovBind);
  handsToggle.addEventListener("change", toggleHands);

  // ---------- game optimization ----------

  const optToggle = $("#optToggle");
  const optStatus = $("#optStatus");
  const optStateBadge = $("#optStateBadge");
  const optRows = $("#optRows");

  function setOptBadge(applied) {
    if (!optStateBadge) return;
    optStateBadge.textContent = applied ? "Вкл" : "Выкл";
    optStateBadge.classList.toggle("on", !!applied);
    optStateBadge.classList.toggle("off", !applied);
  }

  function renderOptRows(options) {
    optRows.innerHTML = "";
    options.forEach((o) => {
      const label = document.createElement("label");
      label.className = "opt-row";

      const info = document.createElement("div");
      info.className = "opt-info";
      const strong = document.createElement("strong");
      strong.textContent = o.title;
      const small = document.createElement("small");
      small.textContent = o.description;
      info.appendChild(strong);
      info.appendChild(small);
      if (Array.isArray(o.commands) && o.commands.length) {
        const cmds = document.createElement("div");
        cmds.className = "opt-cmds";
        cmds.textContent = o.commands.join("  ");
        info.appendChild(cmds);
      }

      const sw = document.createElement("span");
      sw.className = "switch";
      const input = document.createElement("input");
      input.type = "checkbox";
      input.checked = !!o.enabled;
      input.dataset.key = o.key;
      input.addEventListener("change", () => toggleOptOption(input));
      sw.appendChild(input);
      sw.appendChild(document.createElement("span"));

      label.appendChild(info);
      label.appendChild(sw);
      optRows.appendChild(label);
    });
  }

  async function loadOptimization() {
    try {
      const res = await fetch("/api/game/optimization");
      const data = await res.json();
      renderOptRows(Array.isArray(data.options) ? data.options : []);
      optToggle.checked = !!data.applied;
      setOptBadge(data.applied);
      optRows.classList.toggle("hidden", !data.applied);
      if (!data.path) {
        optStatus.textContent = "Укажите папку установки аддонов в настройках.";
        optStatus.classList.remove("good");
      } else {
        optStatus.textContent = data.applied ? "Применяется при запуске игры." : "Не применён.";
        optStatus.classList.toggle("good", !!data.applied);
      }
    } catch {
      optStatus.textContent = "Не удалось получить статус.";
    }
  }

  async function toggleOptOption(input) {
    const key = input.dataset.key;
    const on = input.checked;
    try {
      const res = await fetch("/api/game/optimization/option", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ key, enabled: on })
      });
      const data = await res.json();
      if (data.ok) {
        optStatus.textContent = data.applied
          ? "Применяется при запуске игры."
          : "Изменения применятся при включении оптимизации.";
        optStatus.classList.toggle("good", !!data.applied);
        toast(data.message, "ok");
      } else {
        toast(data.message || "Ошибка сохранения", "err");
        input.checked = !on;
      }
    } catch {
      toast("Ошибка сохранения", "err");
      input.checked = !on;
    }
  }

  optToggle.addEventListener("change", async () => {
    const on = optToggle.checked;
    if (on && optRows.querySelectorAll("input:checked").length === 0) {
      toast("Включите хотя бы один вариант оптимизации", "err");
      optToggle.checked = false;
      return;
    }
    try {
      const res = await fetch("/api/game/optimization", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ enabled: on })
      });
      const data = await res.json();
      if (data.ok) {
        toast(data.message || (on ? "Оптимизация включена" : "Оптимизация выключена"), "ok");
        optStatus.textContent = data.applied ? "Применяется при запуске игры." : "Не применён.";
        optStatus.classList.toggle("good", !!data.applied);
        setOptBadge(data.applied);
        optRows.classList.toggle("hidden", !data.applied);
      } else {
        toast(data.message || "Ошибка применения", "err");
        optToggle.checked = !on;
      }
    } catch {
      toast("Ошибка применения конфига", "err");
      optToggle.checked = !on;
    }
  });

  // ---------- launch parameters ----------

  $("#btnCopyLaunch").addEventListener("click", async () => {
    const text = [...$("#launchCmd").querySelectorAll(".launch-token")]
      .map(t => t.textContent.trim()).filter(Boolean).join(" ");
    try {
      await navigator.clipboard.writeText(text);
      toast("Параметры запуска скопированы", "ok");
    } catch {
      const ta = document.createElement("textarea");
      ta.value = text;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand("copy");
      ta.remove();
      toast("Параметры запуска скопированы", "ok");
    }
  });

  // ---------- bind constructor ----------

  const bindModal = $("#bindModal");  const bindKeyInput = $("#bindKeyInput");
  const bindDescInput = $("#bindDescInput");
  const bindCatInput = $("#bindCatInput");
  const bindCmdInput = $("#bindCmdInput");
  const bindEnabledInput = $("#bindEnabledInput");
  const bindPreview = $("#bindPreview");

  let currentUserName = "";

  const FN_KEYS = ["ESC", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"];
  const MOUSE_KEYS = ["MOUSE1", "MOUSE2", "MOUSE3", "MOUSE4", "MOUSE5", "MWHEELUP", "MWHEELDOWN"];
  const KB_ROWS = [
    ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "BACKSPACE"],
    ["TAB", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\"],
    ["CAPSLOCK", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "ENTER"],
    ["SHIFT", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "SHIFT"],
    ["CTRL", "WIN", "ALT", "SPACE", "ALT", "CTRL"]
  ];
  const KB_WIDTHS = {
    BACKSPACE: 2.2, TAB: 1.6, CAPSLOCK: 1.9, ENTER: 2.2, SHIFT: 2.5, CTRL: 1.5, WIN: 1.2, ALT: 1.2, SPACE: 6, ESC: 1
  };
  const KB_DISPLAY = {
    BACKSPACE: "⌫", TAB: "⇥", CAPSLOCK: "⇪", ENTER: "⏎", SHIFT: "⇧", CTRL: "Ctrl", ALT: "Alt", WIN: "⊞",
    SPACE: "Space", ESC: "Esc", MOUSE1: "ЛКМ", MOUSE2: "ПКМ", MOUSE3: "СКМ", MOUSE4: "Кн 4", MOUSE5: "Кн 5",
    MWHEELUP: "Колесо ↑", MWHEELDOWN: "Колесо ↓"
  };

  function renderKeyPalette() {
    const palette = $("#bindPalette");
    palette.innerHTML = "";

    const mkKey = (k) => {
      const key = document.createElement("button");
      key.type = "button";
      key.className = "kb-key";
      key.textContent = KB_DISPLAY[k] || k;
      key.title = k;
      if (KB_WIDTHS[k]) key.style.flexGrow = KB_WIDTHS[k];
      key.addEventListener("click", () => {
        bindKeyInput.value = k;
        updateBindPreview();
      });
      return key;
    };

    const mkRow = (keys) => {
      const row = document.createElement("div");
      row.className = "kb-row";
      for (const k of keys) row.appendChild(mkKey(k));
      return row;
    };

    const mkGroup = (title, rows) => {
      const group = document.createElement("div");
      group.className = "kb-group";
      const label = document.createElement("span");
      label.className = "kb-title";
      label.textContent = title;
      group.appendChild(label);
      for (const r of rows) group.appendChild(r);
      return group;
    };

    palette.appendChild(mkGroup("Мышка", [mkRow(MOUSE_KEYS)]));
    palette.appendChild(mkGroup("Клавиатура", [mkRow(FN_KEYS), ...KB_ROWS.map(mkRow)]));
  }

  function updateBindPreview() {
    const key = bindKeyInput.value.trim() || "?";
    const command = bindCmdInput.value.trim() || "...";
    bindPreview.textContent = `bind "${key}" "${command}"`;
  }

  function openBindConstructor() {
    bindKeyInput.value = "";
    bindDescInput.value = "";
    bindCatInput.value = "";
    bindCmdInput.value = "";
    bindEnabledInput.checked = false;
    updateBindPreview();
    const dl = $("#bindCats");
    dl.innerHTML = "";
    for (const c of [...new Set(bindsList.map((b) => (b.category || "").trim()).filter(Boolean))]) {
      const opt = document.createElement("option");
      opt.value = c;
      dl.appendChild(opt);
    }
    bindModal.classList.remove("hidden");
    setTimeout(() => bindDescInput.focus(), 60);
  }

  function closeBindConstructor() {
    bindModal.classList.add("hidden");
  }

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && !bindModal.classList.contains("hidden")) closeBindConstructor();
  });

  bindKeyInput.addEventListener("input", updateBindPreview);
  bindCmdInput.addEventListener("input", updateBindPreview);

  $("#bindModalCancel").addEventListener("click", closeBindConstructor);

  bindModal.addEventListener("mousedown", (e) => {
    if (e.target === bindModal) closeBindConstructor();
  });

  $("#bindModalAdd").addEventListener("click", () => {
    const key = bindKeyInput.value.trim();
    const command = bindCmdInput.value.trim();
    if (!key || !command) {
      toast("Заполните клавишу и команду", "err");
      return;
    }
    bindsList.push({
      id: originalBinds.length + bindsList.length + 1,
      key,
      command,
      description: bindDescInput.value.trim(),
      category: bindCatInput.value.trim(),
      enabled: bindEnabledInput.checked,
      favorite: false
    });
    renderFilters();
    renderBinds();
    closeBindConstructor();
    toast("Бинд добавлен", "ok");
  });

  $("#btnAddBind").addEventListener("click", openBindConstructor);
  renderKeyPalette();

  $("#btnDisableAll").addEventListener("click", () => {
    if (!bindsList.some((b) => b.enabled)) {
      toast("Все бинды уже выключены", "info");
      return;
    }
    bindsList.forEach((b) => { b.enabled = false; });
    renderBinds();
    toast("Все бинды выключены. Нажмите «Сохранить»", "ok");
  });

  document.querySelectorAll(".dd-btn").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.stopPropagation();
      const menu = btn.nextElementSibling;
      const willOpen = menu.classList.contains("hidden");
      closeDropdowns();
      if (willOpen) {
        menu.classList.remove("hidden");
        btn.classList.add("open");
      }
    });
  });
  document.addEventListener("click", closeDropdowns);

  $("#btnSaveBinds").addEventListener("click", saveBinds);
  $("#btnSaveGame").addEventListener("click", () => $("#btnSaveBinds").click());

  async function act(id, action) {
    const btn = grid.querySelector(`[data-action="${action}"][data-id="${escapeHtml(id)}"]`);
    if (btn) {
      btn.disabled = true;
      const old = btn.innerHTML;
      btn.innerHTML = '<span class="spinner" style="width:14px;height:14px;margin:0;border-width:2px"></span>';
      try {
        const res = await fetch(`/api/addons/${encodeURIComponent(id)}/${action}`, { method: "POST" });
        const data = await res.json();
        if (data.ok) toast(data.message, "ok");
        else toast(data.message, "err");
      } catch {
        toast("Ошибка соединения с сервером", "err");
      } finally {
        btn.disabled = false;
        btn.innerHTML = old;
        loadAddons();
      }
    }
  }

  // ---------- settings ----------

  function applyAccent(hex) {
    document.documentElement.style.setProperty("--accent", hex);
  }

  function renderColorPicker(selectedHex) {
    const picker = $("#colorPicker");
    picker.innerHTML = "";
    for (const c of PALETTE) {
      const b = document.createElement("button");
      b.type = "button";
      b.className = "swatch" + (c.hex.toLowerCase() === selectedHex.toLowerCase() ? " selected" : "");
      b.style.background = c.hex;
      b.title = c.label;
      b.dataset.hex = c.hex;
      b.addEventListener("click", () => selectColor(c.hex));
      picker.appendChild(b);
    }
    syncPickerToHex(selectedHex);
  }

  function markColorSelected(hex) {
    document.querySelectorAll("#colorPicker .swatch").forEach((s) =>
      s.classList.toggle("selected", s.dataset.hex.toLowerCase() === hex.toLowerCase()));
  }

  // ---------- custom color picker ----------

  let pickerHue = 31, pickerSat = 1, pickerVal = 1;

  function hsvToHex(h, s, v) {
    const c = v * s;
    const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
    const m = v - c;
    let r, g, b;
    if (h < 60) [r, g, b] = [c, x, 0];
    else if (h < 120) [r, g, b] = [x, c, 0];
    else if (h < 180) [r, g, b] = [0, c, x];
    else if (h < 240) [r, g, b] = [0, x, c];
    else if (h < 300) [r, g, b] = [x, 0, c];
    else [r, g, b] = [c, 0, x];
    return "#" + [r, g, b].map((n) => Math.round((n + m) * 255).toString(16).padStart(2, "0")).join("");
  }

  function hexToHsv(hex) {
    const r = parseInt(hex.slice(1, 3), 16) / 255;
    const g = parseInt(hex.slice(3, 5), 16) / 255;
    const b = parseInt(hex.slice(5, 7), 16) / 255;
    const max = Math.max(r, g, b), min = Math.min(r, g, b), d = max - min;
    let h = 0;
    if (d !== 0) {
      if (max === r) h = ((g - b) / d) % 6;
      else if (max === g) h = (b - r) / d + 2;
      else h = (r - g) / d + 4;
      h *= 60;
      if (h < 0) h += 360;
    }
    return { h, s: max === 0 ? 0 : d / max, v: max };
  }

  function syncPickerToHex(hex) {
    const { h, s, v } = hexToHsv(hex);
    pickerHue = h; pickerSat = s; pickerVal = v;
    renderSvArea();
    positionSvMarker();
    positionHueThumb();
    $("#hexInput").value = hex.slice(1);
    $("#swatchPreview").style.background = hex;
  }

  function renderSvArea() {
    $("#svArea").style.background =
      `linear-gradient(to top, #000, transparent), linear-gradient(to right, #fff, hsl(${pickerHue}, 100%, 50%))`;
  }

  function positionSvMarker() {
    const m = $("#svMarker");
    m.style.left = (pickerSat * 100) + "%";
    m.style.top = ((1 - pickerVal) * 100) + "%";
  }

  function positionHueThumb() {
    $("#hueThumb").style.left = (pickerHue / 360 * 100) + "%";
  }

  function applyColor(hex) {
    applyAccent(hex);
    markColorSelected(hex);
    saveColor(hex);
    $("#hexInput").value = hex.slice(1);
    $("#swatchPreview").style.background = hex;
  }

  function selectColor(hex) {
    applyColor(hex);
    syncPickerToHex(hex);
  }

  function pickerSetColor() {
    applyColor(hsvToHex(pickerHue, pickerSat, pickerVal));
  }

  const svArea = $("#svArea");
  function svPointer(e) {
    const r = svArea.getBoundingClientRect();
    let x = (e.clientX - r.left) / r.width;
    let y = (e.clientY - r.top) / r.height;
    pickerSat = Math.min(1, Math.max(0, x));
    pickerVal = Math.min(1, Math.max(0, 1 - y));
    positionSvMarker();
    pickerSetColor();
  }
  svArea.addEventListener("pointerdown", (e) => {
    svArea.setPointerCapture(e.pointerId);
    svPointer(e);
  });
  svArea.addEventListener("pointermove", (e) => {
    if (e.buttons & 1) svPointer(e);
  });

  const hueSlider = $("#hueSlider");
  function huePointer(e) {
    const r = hueSlider.getBoundingClientRect();
    let x = (e.clientX - r.left) / r.width;
    pickerHue = Math.min(1, Math.max(0, x)) * 360;
    renderSvArea();
    positionHueThumb();
    pickerSetColor();
  }
  hueSlider.addEventListener("pointerdown", (e) => {
    hueSlider.setPointerCapture(e.pointerId);
    huePointer(e);
  });
  hueSlider.addEventListener("pointermove", (e) => {
    if (e.buttons & 1) huePointer(e);
  });

  function applyHexInput() {
    let v = $("#hexInput").value.trim().replace(/^#/, "");
    if (/^[0-9a-fA-F]{6}$/.test(v)) {
      selectColor("#" + v.toLowerCase());
    } else {
      toast("Некорректный hex-цвет", "err");
      syncPickerToHex(document.documentElement.style.getPropertyValue("--accent") || DEFAULT_ACCENT);
    }
  }

  $("#hexApply").addEventListener("click", applyHexInput);
  $("#hexInput").addEventListener("keydown", (e) => {
    if (e.key === "Enter") applyHexInput();
  });

  $("#swatchOpen").addEventListener("click", (e) => {
    e.stopPropagation();
    $("#colorPopover").classList.toggle("hidden");
  });

  document.addEventListener("pointerdown", (e) => {
    if (!e.target.closest("#colorPopover") && !e.target.closest("#swatchOpen")) {
      $("#colorPopover").classList.add("hidden");
    }
  });

  async function saveColor(hex) {
    try {
      await fetch("/api/settings", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ accentColor: hex })
      });
    } catch {
      toast("Не удалось сохранить цвет", "err");
    }
  }

  async function loadSettings() {
    try {
      const res = await fetch("/api/settings");
      const data = await res.json();

      targetPathEl.textContent = data.targetPath || "Папка установки не задана";
      targetPathEl.classList.toggle("bad", !!data.error);
      targetPathEl.title = data.error ? data.error : "Папка установки";
      $("#targetPathInput").value = data.configuredPath || "";

      if (data.error && data.autoDetected) {
        $("#targetHint").textContent = `${data.error} Найдено автоматически: ${data.autoDetected}. Нажмите «Найти Steam» или укажите папку вручную.`;
        $("#targetHint").classList.add("bad");
      } else if (!data.configuredPath && data.autoDetected) {
        $("#targetHint").textContent = `Garry's Mod найден автоматически: ${data.autoDetected}`;
        $("#targetHint").classList.remove("bad");
      } else if (data.error) {
        $("#targetHint").textContent = data.error;
        $("#targetHint").classList.add("bad");
      } else {
        $("#targetHint").textContent = "Папка существует — можно устанавливать аддоны.";
        $("#targetHint").classList.remove("bad");
      }

      const accent = data.accentColor || DEFAULT_ACCENT;
      applyAccent(accent);
      renderColorPicker(accent);
    } catch {
      toast("Не удалось получить настройки", "err");
    }
  }

  $("#btnSavePath").addEventListener("click", async () => {
    const btn = $("#btnSavePath");
    btn.disabled = true;
    try {
      const res = await fetch("/api/settings", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ targetPath: $("#targetPathInput").value })
      });
      const data = await res.json();
      if (data.ok) {
        toast("Папка сохранена", "ok");
        loadSettings();
      }
    } catch {
      toast("Не удалось сохранить настройки", "err");
    } finally {
      btn.disabled = false;
    }
  });

  $("#btnDetect").addEventListener("click", async () => {
    const btn = $("#btnDetect");
    btn.disabled = true;
    btn.textContent = "Поиск...";
    try {
      const res = await fetch("/api/settings/detect");
      const data = await res.json();
      if (data.path) {
        $("#targetPathInput").value = data.path;
        $("#targetHint").textContent = `Найдено: ${data.path}`;
        $("#targetHint").classList.remove("bad");
        toast("Steam и Garry's Mod найдены", "ok");
      } else {
        $("#targetHint").textContent = "Garry's Mod не обнаружен. Укажите папку вручную.";
        $("#targetHint").classList.add("bad");
        toast("Garry's Mod не найден", "err");
      }
    } catch {
      toast("Ошибка поиска Steam", "err");
    } finally {
      btn.disabled = false;
      btn.textContent = "Найти Steam";
    }
  });

  // ---------- auth ----------

  let pollTimer = null;

  async function startDiscordLogin() {
    try {
      const res = await fetch("/api/auth/discord/begin");
      const data = await res.json();
      if (!data.url) {
        toast(data.error || "Discord не настроен", "err");
        return;
      }

      if (window.chrome?.webview?.hostObjects?.adonisBridge) {
        await window.chrome.webview.hostObjects.adonisBridge.OpenBrowser(data.url);
      } else {
        window.open(data.url, "_blank");
      }

      toast("Разрешите вход в Discord во вкладке", "info");
      pollLogin(data.loginId);
    } catch {
      toast("Не удалось открыть Discord", "err");
    }
  }

  function pollLogin(loginId) {
    if (pollTimer) clearInterval(pollTimer);
    let tries = 0;
    pollTimer = setInterval(async () => {
      try {
        const res = await fetch(`/api/auth/discord/poll?loginId=${encodeURIComponent(loginId)}`);
        const data = await res.json();
        if (data.authenticated) {
          clearInterval(pollTimer);
          toast("Вход выполнен", "ok");
          loadAuth();
          return;
        }
        if (data.error) {
          clearInterval(pollTimer);
          toast("Вход не выполнен, попробуйте ещё раз", "err");
          loadAuth();
          return;
        }
      } catch {
        /* keep polling */
      }
      if (++tries > 80) {
        clearInterval(pollTimer);
        toast("Время входа истекло", "err");
      }
    }, 1500);
  }

  function userChipHtml(user) {
    return `
      <div class="user-chip" title="Discord">
        ${user.avatarUrl
          ? `<img src="${escapeHtml(user.avatarUrl)}" alt="">`
          : `<span class="avatar-fallback">${escapeHtml((user.name || "?").charAt(0).toUpperCase())}</span>`}
        <span class="user-name">${escapeHtml(user.name)}</span>
      </div>`;
  }

  const logoutBtnHtml = `<button class="btn ghost" id="btnLogout" type="button">${icons.logout}Выйти</button>`;
  const discordLoginHtml = `<button class="login-discord" id="btnDiscordLogin" type="button">${icons.discord}Войти через Discord</button>`;
  const guestChipHtml = `<div class="user-chip guest" title="Гость">${icons.user}<span class="user-name">Гость</span></div>`;

  function showWelcome(visible) {
    $("#welcome").classList.toggle("hidden", !visible);
  }

  function bindAuthButtons() {
    document.querySelectorAll("#btnLogout").forEach((btn) =>
      btn.addEventListener("click", async () => {
        await fetch("/api/auth/logout", { method: "POST" });
        toast("Вы вышли из аккаунта", "info");
        loadAuth();
      }));
    document.querySelectorAll("#btnDiscordLogin").forEach((btn) =>
      btn.addEventListener("click", startDiscordLogin));
  }

  async function loadAuth() {
    try {
      const res = await fetch("/api/auth/status");
      const data = await res.json();
      const top = $("#userArea");
      const acc = $("#settingsAccount");
      const isGuest = !!data.guest;

      currentUserName = (data.user && data.user.name) || (isGuest ? "Гость" : "");

      showWelcome(!data.authenticated && !isGuest);

      if (data.authenticated && data.user) {
        top.innerHTML = userChipHtml(data.user) + logoutBtnHtml;
        acc.innerHTML = userChipHtml(data.user) +
          '<p class="hint">Вы вошли в аккаунт</p>' + logoutBtnHtml;
      } else if (data.discordConfigured) {
        top.innerHTML = (isGuest ? guestChipHtml : "") + discordLoginHtml;
        acc.innerHTML = '<p class="hint">Вход откроется в вашем браузере</p>' + discordLoginHtml;
      } else {
        top.innerHTML = isGuest
          ? guestChipHtml
          : '<span class="auth-hint">Аккаунты не настроены</span>';
        acc.innerHTML = '<p class="hint">Discord-приложение не настроено. Заполните auth.json.</p>';
      }
      bindAuthButtons();
    } catch {
      toast("Не удалось получить статус авторизации", "err");
    }
  }

  $("#welcomeLogin").addEventListener("click", startDiscordLogin);
  $("#welcomeGuest").addEventListener("click", async () => {
    try {
      await fetch("/api/auth/guest", { method: "POST" });
      toast("Вошли как гость", "ok");
    } catch {
      toast("Не удалось войти как гость", "err");
    }
    loadAuth();
  });

  if (new URLSearchParams(location.search).get("auth") === "error") {
    toast("Ошибка входа через Discord", "err");
    history.replaceState(null, "", "/");
  }

  $("#themeLight")?.addEventListener("change", (e) => applyTheme(e.target.checked));

  // ---------- update ----------

  let updateScriptPath = "";

  function setUpdateStatus(text, isError) {
    const el = $("#updateStatus");
    if (!el) return;
    el.textContent = text;
    el.classList.toggle("bad", !!isError);
  }

  async function loadVersion() {
    try {
      const res = await fetch("/api/version");
      const data = await res.json();
      if (data.version) $("#verBadge").textContent = `v${data.version}`;
    } catch {}
  }

  async function checkUpdate(force) {
    setUpdateStatus("Проверка...");
    try {
      const res = await fetch(`/api/update${force ? "?force=true" : ""}`);
      const data = await res.json();
      $("#verBadge").textContent = `v${data.currentVersion}`;

      if (data.hasUpdate) {
        setUpdateStatus(`Доступна версия v${data.latestVersion}. Установить обновление?`);
        const btn = $("#btnCheckUpdate");
        btn.innerHTML = `${icons.install}Обновить`;
        btn.classList.add("primary");
        btn.dataset.update = "1";
      } else {
        setUpdateStatus(`Установлена актуальная версия v${data.currentVersion}.`);
        const btn = $("#btnCheckUpdate");
        btn.innerHTML = `${icons.info}Проверить обновления`;
        btn.classList.remove("primary");
        btn.dataset.update = "";
      }
    } catch {
      setUpdateStatus("Не удалось проверить обновления.", true);
    }
  }

  $("#btnCheckUpdate").addEventListener("click", async (e) => {
    const btn = e.currentTarget;
    if (btn.dataset.update === "1") {
      btn.disabled = true;
      setUpdateStatus("Загрузка обновления...");
      try {
        const res = await fetch("/api/update/apply");
        const data = await res.json();
        if (data.ok) {
          updateScriptPath = data.data;
          setUpdateStatus("Обновление загружено. Приложение будет перезапущено...");
          setTimeout(async () => {
            try {
              await fetch("/api/update/restart", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(updateScriptPath)
              });
            } catch {}
          }, 500);
        } else {
          setUpdateStatus(data.message || "Ошибка обновления.", true);
          btn.disabled = false;
        }
      } catch {
        setUpdateStatus("Ошибка загрузки обновления.", true);
        btn.disabled = false;
      }
    } else {
      checkUpdate(true);
    }
  });

  loadAddons();
  loadSettings();
  loadAuth();
  loadTheme();
  loadVersion();
  checkUpdate(false);
})();
