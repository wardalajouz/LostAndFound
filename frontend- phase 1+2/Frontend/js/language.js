// Shared language preference for Findly pages. Only the language is stored.
function getLanguage() {
  try {
    return localStorage.getItem("findlyLanguage") === "ar" ? "ar" : "en";
  } catch (error) {
    return "en";
  }
}

function saveLanguage(language) {
  document.documentElement.lang = language;
  document.documentElement.dir = language === "ar" ? "rtl" : "ltr";
  try {
    localStorage.setItem("findlyLanguage", language);
  } catch (error) {
    // The page still works if the browser blocks storage.
  }
}
