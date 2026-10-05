(() => {
  const filterButtons = [...document.querySelectorAll("[data-gallery-filter]")];
  const posts = [...document.querySelectorAll("[data-gallery-category]")];
  const resultTitle = document.getElementById("galleryResultTitle");
  const resultCount = document.getElementById("galleryResultCount");
  const pagination = document.getElementById("galleryPagination");
  const pageNumbers = document.getElementById("galleryPageNumbers");
  const previousButton = pagination?.querySelector('[data-gallery-page="previous"]');
  const nextButton = pagination?.querySelector('[data-gallery-page="next"]');

  const formatNumber = value => window.HNUI.formatNumber(value);
  const setResultCount = value => {
    resultCount.replaceChildren();
    const number = document.createElement("b");
    const label = document.createElement("small");
    number.textContent = formatNumber(value);
    label.textContent = "اثر";
    resultCount.append(number, label);
  };
  const pageSize = 12;
  let activeFilter = "all";
  let activePage = 1;

  const filteredPosts = () => posts.filter(post =>
    activeFilter === "all" || post.dataset.galleryCategory === activeFilter
  );

  const renderPagination = () => {
    if (!pagination || !pageNumbers || !previousButton || !nextButton) return;
    const totalPages = Math.max(1, Math.ceil(filteredPosts().length / pageSize));
    activePage = Math.min(activePage, totalPages);
    pageNumbers.replaceChildren();

    for (let page = 1; page <= totalPages; page += 1) {
      const button = document.createElement("button");
      button.type = "button";
      button.dataset.galleryPage = String(page);
      button.textContent = formatNumber(page);
      button.setAttribute("aria-label", `صفحه ${formatNumber(page)}`);
      button.setAttribute("aria-current", page === activePage ? "page" : "false");
      button.classList.toggle("is-active", page === activePage);
      button.addEventListener("click", () => {
        activePage = page;
        renderGallery(true);
      });
      pageNumbers.append(button);
    }

    previousButton.disabled = activePage === 1;
    nextButton.disabled = activePage === totalPages;
    pagination.hidden = totalPages <= 1;
  };

  const renderGallery = (scrollToResults = false) => {
    const matches = filteredPosts();
    const pageStart = (activePage - 1) * pageSize;
    const visiblePosts = new Set(matches.slice(pageStart, pageStart + pageSize));

    posts.forEach(post => { post.hidden = !visiblePosts.has(post); });
    setResultCount(matches.length);
    renderPagination();

    if (scrollToResults) {
      window.requestAnimationFrame(() => {
        const resultsHead = document.querySelector(".gallery-results-head");
        if (!resultsHead) return;
        const headerOffset = 96;
        const top = resultsHead.getBoundingClientRect().top + window.scrollY - headerOffset;
        window.scrollTo({ top: Math.max(0, top), behavior: "smooth" });
      });
    }
  };

  if (filterButtons.length && posts.length && resultTitle && resultCount) {
    filterButtons.forEach(button => button.addEventListener("click", () => {
      activeFilter = button.dataset.galleryFilter;
      activePage = 1;

      filterButtons.forEach(item => {
        const active = item === button;
        item.classList.toggle("is-active", active);
        item.setAttribute("aria-pressed", String(active));
      });

      resultTitle.textContent = activeFilter === "all"
        ? "همه آثار گالری"
        : button.querySelector("b")?.textContent ?? "آثار گالری";
      renderGallery();
    }));

    previousButton?.addEventListener("click", () => {
      if (activePage <= 1) return;
      activePage -= 1;
      renderGallery(true);
    });

    nextButton?.addEventListener("click", () => {
      const totalPages = Math.ceil(filteredPosts().length / pageSize);
      if (activePage >= totalPages) return;
      activePage += 1;
      renderGallery(true);
    });

    renderGallery();
  }

  const dialog = document.getElementById("galleryPlayer");
  const video = document.getElementById("galleryVideo");
  const playerTitle = document.getElementById("galleryPlayerTitle");
  const progress = document.getElementById("galleryPlayerProgress");
  const currentTime = document.getElementById("galleryPlayerCurrent");
  const duration = document.getElementById("galleryPlayerDuration");

  if (!dialog || !video || !playerTitle || !progress || !currentTime || !duration) return;

  const toggleButtons = [...dialog.querySelectorAll("[data-player-toggle]")];
  const muteButton = dialog.querySelector("[data-player-mute]");
  const fullscreenButton = dialog.querySelector("[data-player-fullscreen]");
  const closeButton = dialog.querySelector("[data-player-close]");

  const formatTime = seconds => {
    if (!Number.isFinite(seconds)) return "۰:۰۰";
    const minutes = Math.floor(seconds / 60);
    const remaining = Math.floor(seconds % 60).toString().padStart(2, "0");
    return `${minutes}:${remaining}`.replace(/\d/g, digit => "۰۱۲۳۴۵۶۷۸۹"[digit]);
  };

  const syncPlayback = () => {
    const playing = !video.paused && !video.ended;
    dialog.classList.toggle("is-playing", playing);
    toggleButtons.forEach(button => {
      button.querySelector("i").className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
      button.setAttribute("aria-label", playing ? "توقف ویدئو" : "پخش ویدئو");
    });
  };

  const togglePlayback = async () => {
    if (video.paused || video.ended) await video.play();
    else video.pause();
  };

  const closePlayer = () => {
    video.pause();
    video.removeAttribute("src");
    video.load();
    dialog.close();
  };

  document.querySelectorAll("[data-gallery-video]").forEach(trigger => {
    trigger.addEventListener("click", () => {
      video.src = trigger.dataset.galleryVideo;
      video.poster = trigger.dataset.galleryPoster ?? "";
      playerTitle.textContent = trigger.dataset.galleryTitle ?? "نمایش ویدئو";
      progress.value = "0";
      currentTime.textContent = "۰:۰۰";
      duration.textContent = "۰:۰۰";
      dialog.showModal();
      closeButton?.focus();
    });
  });

  toggleButtons.forEach(button => button.addEventListener("click", togglePlayback));
  video.addEventListener("click", togglePlayback);
  video.addEventListener("play", syncPlayback);
  video.addEventListener("pause", syncPlayback);
  video.addEventListener("ended", syncPlayback);

  video.addEventListener("loadedmetadata", () => {
    duration.textContent = formatTime(video.duration);
  });

  video.addEventListener("timeupdate", () => {
    const percent = video.duration ? (video.currentTime / video.duration) * 100 : 0;
    progress.value = String(percent);
    currentTime.textContent = formatTime(video.currentTime);
  });

  progress.addEventListener("input", () => {
    if (video.duration) video.currentTime = (Number(progress.value) / 100) * video.duration;
  });

  muteButton?.addEventListener("click", () => {
    video.muted = !video.muted;
    muteButton.querySelector("i").className = video.muted ? "bi bi-volume-mute" : "bi bi-volume-up";
    muteButton.setAttribute("aria-label", video.muted ? "وصل‌کردن صدا" : "قطع صدا");
  });

  fullscreenButton?.addEventListener("click", async () => {
    if (document.fullscreenElement) await document.exitFullscreen();
    else await dialog.querySelector(".gallery-player-stage")?.requestFullscreen();
  });

  closeButton?.addEventListener("click", closePlayer);
  dialog.addEventListener("cancel", event => {
    event.preventDefault();
    closePlayer();
  });
})();
