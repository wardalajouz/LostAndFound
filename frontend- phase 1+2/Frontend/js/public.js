// Findly Phase 2: simple public-page UI interactions only.
const categories = ["Electronics", "Bags", "Documents", "Keys", "Accessories", "Clothing", "Books", "Other"];

const words = {
  en: {
    home: "Home",
    mainNavigation: "Main navigation",
    footerNavigation: "Footer links",
    browse: "Browse Items",
    how: "How It Works",
    about: "About Us",
    help: "Help",
    login: "Login",
    signup: "Sign Up",
    contact: "Contact Support",
    footerText: "Yarmouk University lost & found community.",
    previewNote: "Frontend UI preview — no information is saved or sent.",
    futurePhase: "Terms and Privacy pages will be added in Phase 6.",
    privacy: "Privacy",
    terms: "Terms",
    loginRequired: "Login Required",
    loginRequiredText: "Please log in to create a report or message the owner.",
    cancel: "Cancel",
    campusTag: "YARMOUK UNIVERSITY",
    heroTitle: "Lost something? Let's find it.",
    heroDescription: "One place to search for missing belongings and report items found on campus.",
    search: "Search",
    searchHint: "Search for an item name or description",
    reportLost: "Report Lost Item",
    reportFound: "Report Found Item",
    campusMembers: "Students · Faculty · Staff · Visitors",
    recentLost: "Recent Lost Items",
    recentFound: "Recent Found Items",
    seeAll: "View All",
    noLiveItems: "No real reports to show yet",
    itemsLater: "Real item records will appear here when the team connects the backend.",
    howTitle: "How Findly Works",
    step1: "Search",
    step1Text: "Browse the lost and found reports on campus.",
    step2: "Report",
    step2Text: "After signing in, publish details about a lost or found item.",
    step3: "Connect",
    step3Text: "Talk to the report owner using Findly's private chat.",
    learnMore: "Learn how it works",
    aiTitle: "AI-powered assistance",
    aiText: "Image descriptions and smart matching are planned features. No AI result is simulated here.",
    uiPreview: "UI PREVIEW",
    explore: "Explore Items",
    browseTitle: "Browse Items",
    lostItems: "Lost Items",
    foundItems: "Found Items",
    browseDescription: "Search and filter Yarmouk University reports. Results will appear after integration.",
    type: "Report type",
    category: "Category",
    allCategories: "All categories",
    location: "Campus location",
    locationsLater: "Locations will load from backend",
    date: "Date",
    sort: "Sort By",
    newest: "Newest First",
    oldest: "Oldest First",
    apply: "Apply Filters",
    clear: "Clear Filters",
    results: "Results",
    createReport: "Create a Report",
    filtersPreview: "Filters selected. Real searching and sorting will be enabled after integration.",
    backBrowse: "Back to Browse",
    itemDetails: "Item Details",
    detailsDescription: "Public report details will appear when real posts are connected.",
    photoLater: "Item photo placeholder",
    itemNotLoaded: "No item loaded yet",
    itemName: "Item Name",
    status: "Status",
    messageOwner: "Message Owner",
    aboutTitle: "About Findly",
    aboutLead: "Findly helps the Yarmouk University community reconnect with their belongings.",
    ourMission: "Our Mission",
    ourMissionText: "Make it easier to report lost and found items and find the right person, without showing private contact details.",
    ourCommunity: "Our Community",
    ourCommunityText: "Designed for students, faculty, staff and visitors at Yarmouk University.",
    readyToStart: "Ready to get started?",
    howLead: "Three simple steps to use Findly.",
    helpTitle: "Help & Support",
    helpLead: "Find answers or contact the Findly support team.",
    commonQuestions: "Common Questions",
    faqHelpText: "Read answers about posting, browsing and contacting people.",
    viewFaq: "View FAQ",
    contactHelpText: "Need more help? Open the support contact form.",
    needAccount: "Want to report an item?",
    faqTitle: "Frequently Asked Questions",
    faqLead: "Helpful answers about Findly.",
    searchFaq: "Search questions",
    noFaq: "No matching questions found.",
    stillQuestions: "Still have questions?",
    contactTitle: "Contact Support",
    contactLead: "Write a question or problem report. This form is UI-only for now.",
    fullName: "Full Name",
    email: "Email",
    subject: "Subject",
    message: "Message",
    send: "Send Message",
    contactInvalid: "Please complete all fields with a valid email.",
    contactPreview: "Form looks good. No message was sent in this UI preview.",
    all: "All",
    lost: "Lost",
    found: "Found"
  },
  ar: {
    home: "الرئيسية",
    mainNavigation: "القائمة الرئيسية",
    footerNavigation: "روابط التذييل",
    browse: "تصفح الأغراض",
    how: "كيف يعمل",
    about: "من نحن",
    help: "المساعدة",
    login: "تسجيل الدخول",
    signup: "إنشاء حساب",
    contact: "تواصل مع الدعم",
    footerText: "مجتمع المفقودات والموجودات في جامعة اليرموك.",
    previewNote: "معاينة واجهة فقط — لا يتم حفظ أو إرسال معلومات.",
    futurePhase: "صفحتا الخصوصية والشروط ستضافان في المرحلة السادسة.",
    privacy: "الخصوصية",
    terms: "الشروط",
    loginRequired: "تسجيل الدخول مطلوب",
    loginRequiredText: "يرجى تسجيل الدخول لإنشاء بلاغ أو مراسلة صاحب المنشور.",
    cancel: "إلغاء",
    campusTag: "جامعة اليرموك",
    heroTitle: "ضاع منك غرض؟ خلينا نلاقيه.",
    heroDescription: "مكان واحد للبحث عن المفقودات والإبلاغ عن الأغراض التي تم العثور عليها داخل الجامعة.",
    search: "بحث",
    searchHint: "ابحثي باسم الغرض أو وصفه",
    reportLost: "الإبلاغ عن غرض مفقود",
    reportFound: "الإبلاغ عن غرض موجود",
    campusMembers: "طلبة · هيئة تدريس · موظفون · زوار",
    recentLost: "أحدث المفقودات",
    recentFound: "أحدث الموجودات",
    seeAll: "عرض الكل",
    noLiveItems: "لا توجد بلاغات حقيقية للعرض حالياً",
    itemsLater: "ستظهر البلاغات الحقيقية بعد ربط الواجهة بالباك إند.",
    howTitle: "كيف يعمل فايندلي؟",
    step1: "ابحث",
    step1Text: "تصفح بلاغات المفقودات والموجودات داخل الجامعة.",
    step2: "بلّغ",
    step2Text: "بعد تسجيل الدخول، أنشئ بلاغاً عن غرض مفقود أو موجود.",
    step3: "تواصل",
    step3Text: "تواصل مع صاحب المنشور عبر المحادثة الخاصة داخل فايندلي.",
    learnMore: "اعرف المزيد",
    aiTitle: "مساعدة بالذكاء الاصطناعي",
    aiText: "وصف الصور والمطابقة الذكية من الميزات المخطط لها، ولا نعرض نتائج AI وهمية.",
    uiPreview: "معاينة واجهة",
    explore: "استعرض الأغراض",
    browseTitle: "تصفح الأغراض",
    lostItems: "المفقودات",
    foundItems: "الموجودات",
    browseDescription: "البحث والفلترة في بلاغات جامعة اليرموك. تظهر النتائج بعد الربط.",
    type: "نوع البلاغ",
    category: "التصنيف",
    allCategories: "كل التصنيفات",
    location: "الموقع في الجامعة",
    locationsLater: "ستُجلب المواقع من الباك إند",
    date: "التاريخ",
    sort: "الترتيب",
    newest: "الأحدث أولاً",
    oldest: "الأقدم أولاً",
    apply: "تطبيق الفلاتر",
    clear: "مسح الفلاتر",
    results: "النتائج",
    createReport: "إنشاء بلاغ",
    filtersPreview: "تم اختيار الفلاتر. سيعمل البحث والترتيب الحقيقي بعد الربط.",
    backBrowse: "العودة للتصفح",
    itemDetails: "تفاصيل الغرض",
    detailsDescription: "تظهر تفاصيل البلاغات العامة بعد ربط المنشورات الحقيقية.",
    photoLater: "مكان صورة الغرض",
    itemNotLoaded: "لم يتم تحميل غرض بعد",
    itemName: "اسم الغرض",
    status: "الحالة",
    messageOwner: "راسل صاحب المنشور",
    aboutTitle: "عن فايندلي",
    aboutLead: "يساعد فايندلي مجتمع جامعة اليرموك في العثور على أغراضهم.",
    ourMission: "رسالتنا",
    ourMissionText: "تسهيل الإبلاغ عن المفقودات والموجودات والتواصل من غير إظهار بيانات الاتصال الخاصة.",
    ourCommunity: "مجتمعنا",
    ourCommunityText: "صُمّم لطلبة جامعة اليرموك وأعضاء هيئة التدريس والموظفين والزوار.",
    readyToStart: "جاهزة للبدء؟",
    howLead: "ثلاث خطوات بسيطة لاستخدام فايندلي.",
    helpTitle: "المساعدة والدعم",
    helpLead: "اعرف الإجابات أو تواصل مع فريق الدعم.",
    commonQuestions: "أسئلة شائعة",
    faqHelpText: "إجابات عن البلاغات والتصفح والتواصل.",
    viewFaq: "عرض الأسئلة الشائعة",
    contactHelpText: "تحتاج مساعدة إضافية؟ افتح نموذج التواصل مع الدعم.",
    needAccount: "بدك تبلغ عن غرض؟",
    faqTitle: "الأسئلة الشائعة",
    faqLead: "إجابات مفيدة عن فايندلي.",
    searchFaq: "ابحث في الأسئلة",
    noFaq: "لا توجد أسئلة مطابقة.",
    stillQuestions: "عندك أسئلة أخرى؟",
    contactTitle: "تواصل مع الدعم",
    contactLead: "اكتب سؤالاً أو مشكلة. النموذج واجهة فقط حالياً.",
    fullName: "الاسم الكامل",
    email: "البريد الإلكتروني",
    subject: "الموضوع",
    message: "الرسالة",
    send: "إرسال",
    contactInvalid: "يرجى إكمال جميع الحقول وإدخال بريد إلكتروني صحيح.",
    contactPreview: "الحقول صحيحة. لم تُرسل أي رسالة في معاينة الواجهة.",
    all: "الكل",
    lost: "مفقود",
    found: "موجود"
  }
};

const faqItems = {
  en: [
    { question: "Can I browse without an account?", answer: "Yes. Guests can browse, search and open public item details." },
    { question: "Do I need a university email to register?", answer: "No. A personal email is allowed." },
    { question: "Is a photo required for every report?", answer: "A Found report needs at least one photo. A Lost report can be submitted without one." },
    { question: "How do I contact another user?", answer: "Registered users can use the private item-related chat. Contact details are not displayed." },
    { question: "Can I hand a found item to security?", answer: "Yes. Found reports include a Handed to Security option." }
  ],
  ar: [
    { question: "بقدر أتصفح بدون حساب؟", answer: "نعم، الضيف يقدر يتصفح ويبحث ويشوف تفاصيل البلاغات العامة." },
    { question: "لازم إيميل الجامعة للتسجيل؟", answer: "لا، مسموح استخدام البريد الإلكتروني الشخصي." },
    { question: "الصورة مطلوبة لكل بلاغ؟", answer: "البلاغ عن غرض موجود يحتاج صورة واحدة على الأقل، أما بلاغ المفقود فالصورة اختيارية." },
    { question: "كيف أتواصل مع مستخدم آخر؟", answer: "المستخدم المسجل بقدر يتواصل عبر المحادثة الخاصة المرتبطة بالغرض، بدون إظهار معلومات الاتصال." },
    { question: "بقدر أسلّم الغرض الموجود للأمن؟", answer: "نعم، بلاغات الموجودات تتضمن خيار تم تسليمه للأمن." }
  ]
};

Vue.createApp({
  data() {
    return {
      page: document.body.dataset.page,
      language: getLanguage(),
      showLoginModal: false,
      searchText: "",
      typeFilter: "All",
      categoryFilter: "All",
      campusLocationId: "",
      dateFilter: "",
      sortFilter: "newest",
      categories,
      filterMessage: "",
      faqSearch: "",
      openFaq: -1,
      contactForm: { fullName: "", email: "", subject: "", message: "" },
      contactError: "",
      contactNotice: ""
    };
  },
  computed: {
    t() { return words[this.language]; },
    isBrowse() { return ["browse", "lost", "found"].includes(this.page); },
    filteredFaqs() {
      const search = this.faqSearch.toLowerCase();
      return faqItems[this.language].filter(faq => faq.question.toLowerCase().includes(search));
    }
  },
  mounted() {
    this.updatePageLanguage();
    const params = new URLSearchParams(window.location.search);
    this.searchText = params.get("search") || "";
    this.typeFilter = this.page === "lost" ? "Lost" : this.page === "found" ? "Found" : (params.get("type") || "All");
  },
  methods: {
    updatePageLanguage() {
      saveLanguage(this.language);
      const titles = {
        home: "home", browse: "browseTitle", lost: "lostItems",
        found: "foundItems", "post-details": "itemDetails",
        about: "aboutTitle", help: "helpTitle", faq: "faqTitle",
        "how-it-works": "howTitle", contact: "contactTitle"
      };
      document.title = this.t[titles[this.page]] + " | Findly";
    },
    toggleLanguage() {
      this.language = this.language === "en" ? "ar" : "en";
      this.updatePageLanguage();
      this.openFaq = -1;
    },
    openLoginModal() { this.showLoginModal = true; },
    openBrowse() {
      const query = new URLSearchParams();
      if (this.searchText) query.set("search", this.searchText);
      window.location.href = "browse.html" + (query.toString() ? "?" + query.toString() : "");
    },
    kindLabel(kind) { return this.t[kind.toLowerCase()]; },
    categoryLabel(category) {
      const ar = {
        Electronics: "إلكترونيات", Bags: "حقائب", Documents: "مستندات",
        Keys: "مفاتيح", Accessories: "إكسسوارات", Clothing: "ملابس",
        Books: "كتب", Other: "أخرى"
      };
      return this.language === "ar" ? ar[category] : category;
    },
    applyFilters() { this.filterMessage = this.t.filtersPreview; },
    clearFilters() {
      this.searchText = "";
      this.typeFilter = "All";
      this.categoryFilter = "All";
      this.campusLocationId = "";
      this.dateFilter = "";
      this.sortFilter = "newest";
      this.filterMessage = "";
    },
    checkContact() {
      this.contactError = "";
      this.contactNotice = "";
      const form = this.contactForm;
      if (!form.fullName || !form.email.includes("@") || !form.subject || !form.message) {
        this.contactError = this.t.contactInvalid;
        return;
      }
      this.contactNotice = this.t.contactPreview;
    }
  }
}).mount("#app");
