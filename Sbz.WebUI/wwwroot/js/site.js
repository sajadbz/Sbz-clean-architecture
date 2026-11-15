const navigationToggle = document.getElementById("navbar-toggle");
const navigation = document.getElementById("site-navigation");

navigationToggle.addEventListener("click", () => {
    const expanded = navigation.classList.toggle("show");
    navigationToggle.setAttribute("aria-expanded", String(expanded));
});
