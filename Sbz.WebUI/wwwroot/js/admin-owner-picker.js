const picker = document.querySelector("[data-owner-picker]");

if (picker) {
    const searchRegion = picker.querySelector("[data-owner-search]");
    const search = searchRegion.querySelector("input");
    const status = searchRegion.querySelector("[data-owner-matches]");
    const select = picker.querySelector("select");
    const options = Array.from(select.options);

    searchRegion.hidden = false;
    search.addEventListener("input", () => {
        const query = search.value.trim().toLowerCase();
        const selected = select.value;
        let matches = 0;
        const visibleOptions = options.filter(option => {
            if (!option.value)
                return true;

            const matchesQuery = option.text.toLowerCase().includes(query);
            if (matchesQuery)
                matches++;

            return matchesQuery || option.value === selected;
        });

        select.replaceChildren(...visibleOptions);
        select.value = selected;
        if (!query)
            status.textContent = "";
        else if (matches === 1)
            status.textContent = "1 matching account.";
        else
            status.textContent = matches === 0 ? "No matching accounts." : `${matches} matching accounts.`;
    });
}
