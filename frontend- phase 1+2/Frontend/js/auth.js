// Findly: authentication UI only (no API requests or authentication storage).
const userTypes = ["Student", "Faculty", "Staff", "Visitor"];

const authWords = {
  "en": {
    "university": "YARMOUK UNIVERSITY",
    "introTitle": "Lost something? Let's find it.",
    "introDescription": "One place for lost and found belongings on campus.",
    "introResetTitle": "Find your way back.",
    "introResetDescription": "Set a new password for your Findly account.",
    "introAlt": "Magnifying glass, backpack, books and other campus belongings",
    "introResetAlt": "Magnifying glass and campus belongings",
    "campusMembers": "Students · Faculty · Staff · Visitors",
    "switchLanguage": "Switch language",
    "loginTitle": "Sign In",
    "loginDescription": "Welcome back to Findly.",
    "registerTitle": "Create Account",
    "registerDescription": "Join Findly using your personal email. No phone number needed.",
    "forgotTitle": "Forgot Password?",
    "forgotDescription": "Enter your email to request password reset instructions.",
    "resetTitle": "Reset Password",
    "resetDescription": "Use the email and reset token from the link sent to your email.",
    "email": "Email",
    "emailPlaceholder": "you@example.com",
    "password": "Password",
    "passwordPlaceholder": "Your password",
    "showPassword": "Show password",
    "showConfirmation": "Show confirmation",
    "signIn": "Sign In",
    "forgotPassword": "Forgot password?",
    "createAccount": "Create account",
    "continueGuest": "Continue as Guest",
    "loginNote": "UI preview: the form checks your input but does not sign in.",
    "fullName": "Full Name",
    "userType": "User Type",
    "selectUserType": "Select your type",
    "student": "Student",
    "faculty": "Faculty",
    "staff": "Staff",
    "visitor": "Visitor",
    "passwordHint": "8 characters or more",
    "confirmPassword": "Confirm Password",
    "agreeTerms": "I agree to the",
    "terms": "Terms & Conditions",
    "alreadyAccount": "Already have an account? Sign In",
    "requestReset": "Request Reset Link",
    "backToLogin": "Back to Sign In",
    "forgotNote": "UI preview: no reset email is sent.",
    "resetToken": "Reset Token",
    "newPassword": "New Password",
    "confirmNewPassword": "Confirm New Password",
    "resetPassword": "Reset Password",
    "requestNewLink": "Request New Link",
    "resetNote": "UI preview: the password is not changed.",
    "termsNotice": "The Terms & Conditions page will be added later.",
    "loginMissing": "Enter your email and password.",
    "loginPreview": "Form looks good. Sign in is not enabled in this UI preview.",
    "nameInvalid": "Full name must be 2–100 characters.",
    "userTypeInvalid": "Choose Student, Faculty, Staff or Visitor.",
    "passwordInvalid": "Password needs 8–128 characters, uppercase, lowercase and a number.",
    "passwordMismatch": "Passwords do not match.",
    "termsMissing": "Please accept the Terms & Conditions.",
    "registerPreview": "Form looks good. No account was created in this UI preview.",
    "forgotMissing": "Enter your email address.",
    "forgotPreview": "Email format looks good. No reset link was sent in this UI preview.",
    "resetMissing": "Enter your email and reset token.",
    "newPasswordInvalid": "New password needs 8–128 characters, uppercase, lowercase and a number.",
    "newPasswordMismatch": "New passwords do not match.",
    "resetPreview": "Form looks good. The password was not changed in this UI preview."
  },
  "ar": {
    "university": "جامعة اليرموك",
    "introTitle": "ضاع منك غرض؟ خلينا نلاقيه.",
    "introDescription": "مكان واحد للمفقودات والموجودات داخل الجامعة.",
    "introResetTitle": "ارجع لحسابك بسهولة.",
    "introResetDescription": "عيّن كلمة مرور جديدة لحسابك في Findly.",
    "introAlt": "رسم لعدسة مكبرة وحقيبة وكتب وأغراض جامعية",
    "introResetAlt": "رسم لعدسة مكبرة وأغراض جامعية",
    "campusMembers": "طلبة · هيئة تدريس · موظفون · زوار",
    "switchLanguage": "تغيير اللغة",
    "loginTitle": "تسجيل الدخول",
    "loginDescription": "أهلاً بعودتك إلى Findly.",
    "registerTitle": "إنشاء حساب",
    "registerDescription": "انضم إلى Findly باستخدام بريدك الإلكتروني الشخصي، بدون رقم هاتف.",
    "forgotTitle": "نسيت كلمة المرور؟",
    "forgotDescription": "أدخل بريدك الإلكتروني لطلب تعليمات إعادة تعيين كلمة المرور.",
    "resetTitle": "إعادة تعيين كلمة المرور",
    "resetDescription": "استخدم البريد الإلكتروني ورمز إعادة التعيين الموجودين في الرابط المرسل إليك.",
    "email": "البريد الإلكتروني",
    "emailPlaceholder": "you@example.com",
    "password": "كلمة المرور",
    "passwordPlaceholder": "كلمة المرور",
    "showPassword": "إظهار كلمة المرور",
    "showConfirmation": "إظهار تأكيد كلمة المرور",
    "signIn": "تسجيل الدخول",
    "forgotPassword": "نسيت كلمة المرور؟",
    "createAccount": "إنشاء حساب",
    "continueGuest": "المتابعة كضيف",
    "loginNote": "معاينة واجهة فقط: يتم التحقق من المدخلات دون تسجيل الدخول فعلياً.",
    "fullName": "الاسم الكامل",
    "userType": "نوع المستخدم",
    "selectUserType": "اختر نوع المستخدم",
    "student": "طالب",
    "faculty": "عضو هيئة تدريس",
    "staff": "موظف",
    "visitor": "زائر",
    "passwordHint": "8 أحرف على الأقل",
    "confirmPassword": "تأكيد كلمة المرور",
    "agreeTerms": "أوافق على",
    "terms": "الشروط والأحكام",
    "alreadyAccount": "لديك حساب؟ سجّل الدخول",
    "requestReset": "طلب رابط إعادة التعيين",
    "backToLogin": "العودة إلى تسجيل الدخول",
    "forgotNote": "معاينة واجهة فقط: لم يتم إرسال بريد لإعادة تعيين كلمة المرور.",
    "resetToken": "رمز إعادة التعيين",
    "newPassword": "كلمة المرور الجديدة",
    "confirmNewPassword": "تأكيد كلمة المرور الجديدة",
    "resetPassword": "إعادة تعيين كلمة المرور",
    "requestNewLink": "طلب رابط جديد",
    "resetNote": "معاينة واجهة فقط: لم يتم تغيير كلمة المرور.",
    "termsNotice": "ستتم إضافة صفحة الشروط والأحكام في مرحلة لاحقة.",
    "loginMissing": "يرجى إدخال البريد الإلكتروني وكلمة المرور.",
    "loginPreview": "المدخلات تبدو صحيحة، لكن تسجيل الدخول غير مفعّل في معاينة الواجهة.",
    "nameInvalid": "يجب أن يتكون الاسم الكامل من 2 إلى 100 حرف.",
    "userTypeInvalid": "يرجى اختيار طالب أو عضو هيئة تدريس أو موظف أو زائر.",
    "passwordInvalid": "كلمة المرور تحتاج من 8 إلى 128 حرفاً، وحرفاً كبيراً وصغيراً ورقماً.",
    "passwordMismatch": "كلمتا المرور غير متطابقتين.",
    "termsMissing": "يرجى الموافقة على الشروط والأحكام.",
    "registerPreview": "المدخلات تبدو صحيحة، لكن لم يتم إنشاء أي حساب في معاينة الواجهة.",
    "forgotMissing": "يرجى إدخال البريد الإلكتروني.",
    "forgotPreview": "صيغة البريد تبدو صحيحة، لكن لم يتم إرسال رابط إعادة تعيين.",
    "resetMissing": "يرجى إدخال البريد الإلكتروني ورمز إعادة التعيين.",
    "newPasswordInvalid": "كلمة المرور الجديدة تحتاج من 8 إلى 128 حرفاً، وحرفاً كبيراً وصغيراً ورقماً.",
    "newPasswordMismatch": "كلمتا المرور الجديدتان غير متطابقتين.",
    "resetPreview": "المدخلات تبدو صحيحة، لكن لم تتغير كلمة المرور في معاينة الواجهة."
  }
};

function strongPassword(password) {
  return password.length >= 8 && password.length <= 128 &&
    /[a-z]/.test(password) && /[A-Z]/.test(password) && /[0-9]/.test(password);
}

Vue.createApp({
  data() {
    return {
      page: document.body.dataset.page,
      language: getLanguage(),
      fullName: "",
      email: "",
      userType: "",
      password: "",
      confirmPassword: "",
      acceptTerms: false,
      resetToken: "",
      newPassword: "",
      confirmNewPassword: "",
      showPassword: false,
      showConfirmPassword: false,
      error: "",
      message: ""
    };
  },
  computed: {
    t() {
      return authWords[this.language];
    }
  },
  mounted() {
    this.updatePageLanguage();
  },
  methods: {
    updatePageLanguage() {
      saveLanguage(this.language);
      const titles = {
        login: "loginTitle", register: "registerTitle",
        forgot: "forgotTitle", reset: "resetTitle"
      };
      document.title = this.t[titles[this.page]] + " | Findly";
    },
    toggleLanguage() {
      this.language = this.language === "en" ? "ar" : "en";
      this.updatePageLanguage();
    },
    clearFeedback() {
      this.error = "";
      this.message = "";
    },
    showTermsNotice() {
      this.clearFeedback();
      this.message = "termsNotice";
    },
    checkLogin() {
      this.clearFeedback();
      if (!this.email.trim() || !this.password) {
        this.error = "loginMissing";
        return;
      }
      this.message = "loginPreview";
    },
    checkRegister() {
      this.clearFeedback();
      if (this.fullName.trim().length < 2 || this.fullName.trim().length > 100) {
        this.error = "nameInvalid";
        return;
      }
      if (!userTypes.includes(this.userType)) {
        this.error = "userTypeInvalid";
        return;
      }
      if (!strongPassword(this.password)) {
        this.error = "passwordInvalid";
        return;
      }
      if (this.password !== this.confirmPassword) {
        this.error = "passwordMismatch";
        return;
      }
      if (!this.acceptTerms) {
        this.error = "termsMissing";
        return;
      }
      this.message = "registerPreview";
    },
    checkForgotPassword() {
      this.clearFeedback();
      if (!this.email.trim()) {
        this.error = "forgotMissing";
        return;
      }
      this.message = "forgotPreview";
    },
    checkResetPassword() {
      this.clearFeedback();
      if (!this.email.trim() || !this.resetToken.trim()) {
        this.error = "resetMissing";
        return;
      }
      if (!strongPassword(this.newPassword)) {
        this.error = "newPasswordInvalid";
        return;
      }
      if (this.newPassword !== this.confirmNewPassword) {
        this.error = "newPasswordMismatch";
        return;
      }
      this.message = "resetPreview";
    }
  }
}).mount("#app");
