const picker = document.querySelector("[data-owner-picker]");

if (picker) {
    const select = picker.querySelector("select");
    const label = picker.querySelector("label");
    const enhanced = picker.querySelector("[data-owner-combobox]");
    const input = picker.querySelector("[role=combobox]");
    const popup = picker.querySelector("[data-owner-popup]");
    const list = picker.querySelector("[role=listbox]");
    const status = picker.querySelector("[data-owner-status]");
    const options = Array.from(select.options);
    let visibleOptions = [];
    let activeIndex = -1;

    function selectedLabel() {
        return select.value ? options.find(option => option.value === select.value)?.text ?? "" : "";
    }

    function close() {
        popup.hidden = true;
        input.value = selectedLabel();
        input.setAttribute("aria-expanded", "false");
        input.removeAttribute("aria-activedescendant");
        activeIndex = -1;
    }

    function choose(option) {
        select.value = option.value;
        close();
    }

    function showOptions(query) {
        const term = query.trim().toLowerCase();
        visibleOptions = options.filter(option => option.text.toLowerCase().includes(term));
        const elements = visibleOptions.map((option, index) => {
            const item = document.createElement("div");
            item.id = `${list.id}-${index}`;
            item.className = "owner-option";
            item.textContent = option.text;
            item.setAttribute("role", "option");
            item.setAttribute("aria-selected", "false");
            item.addEventListener("mousedown", event => event.preventDefault());
            item.addEventListener("click", () => choose(option));
            return item;
        });
        list.replaceChildren(...elements);
        status.textContent = elements.length ? "" : "No matching accounts.";
        popup.hidden = false;
        input.setAttribute("aria-expanded", "true");
        input.removeAttribute("aria-activedescendant");
        activeIndex = -1;
    }

    function moveActive(direction) {
        const count = visibleOptions.length;
        if (!count)
            return;

        if (activeIndex >= 0)
            list.children[activeIndex].setAttribute("aria-selected", "false");
        if (activeIndex < 0)
            activeIndex = direction > 0 ? 0 : count - 1;
        else
            activeIndex = (activeIndex + direction + count) % count;

        const item = list.children[activeIndex];
        item.setAttribute("aria-selected", "true");
        input.setAttribute("aria-activedescendant", item.id);
        item.scrollIntoView({ block: "nearest" });
    }

    input.addEventListener("focus", () => {
        showOptions("");
        input.select();
    });
    input.addEventListener("click", () => {
        if (popup.hidden)
            showOptions("");
        if (input.value === selectedLabel())
            input.select();
    });
    input.addEventListener("input", () => showOptions(input.value));
    input.addEventListener("blur", close);
    input.addEventListener("keydown", event => {
        if (event.isComposing)
            return;

        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            if (popup.hidden)
                showOptions("");
            moveActive(event.key === "ArrowDown" ? 1 : -1);
        } else if (event.key === "Enter" && !popup.hidden) {
            event.preventDefault();
            if (activeIndex >= 0)
                choose(visibleOptions[activeIndex]);
        } else if (event.key === "Escape" && !popup.hidden) {
            event.preventDefault();
            close();
        }
    });

    close();
    label.htmlFor = input.id;
    select.hidden = true;
    enhanced.hidden = false;
}
