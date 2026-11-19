/*
  Findly — Authentication UI only
  Vue handles form values, show/hide password, and local validation.
  Nothing is submitted or saved. No network requests.
*/
const userTypes = ["Student", "Faculty", "Staff", "Visitor"];

function strongPassword(password) {
  return password.length >= 8 && password.length <= 128 &&
    /[a-z]/.test(password) && /[A-Z]/.test(password) && /[0-9]/.test(password);
}

Vue.createApp({
  data() {
    return {
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
  methods: {
    clearFeedback() {
      this.error = "";
      this.message = "";
    },

    showPreviewNotice() {
      this.clearFeedback();
      this.message = "The Home page will be added in the next phase.";
    },

    showTermsNotice() {
      this.clearFeedback();
      this.message = "The Terms & Conditions page will be added later.";
    },

    checkLogin() {
      this.clearFeedback();
      if (!this.email.trim() || !this.password) {
        this.error = "Enter your email and password.";
        return;
      }
      this.message = "Form looks good. Sign in is not enabled in this UI preview.";
    },

    checkRegister() {
      this.clearFeedback();
      if (this.fullName.trim().length < 2 || this.fullName.trim().length > 100) {
        this.error = "Full name must be 2–100 characters.";
        return;
      }
      if (!userTypes.includes(this.userType)) {
        this.error = "Choose Student, Faculty, Staff or Visitor.";
        return;
      }
      if (!strongPassword(this.password)) {
        this.error = "Password needs 8–128 characters, uppercase, lowercase and a number.";
        return;
      }
      if (this.password !== this.confirmPassword) {
        this.error = "Passwords do not match.";
        return;
      }
      if (!this.acceptTerms) {
        this.error = "Please accept the Terms & Conditions.";
        return;
      }
      this.message = "Form looks good. No account was created in this UI preview.";
    },

    checkForgotPassword() {
      this.clearFeedback();
      if (!this.email.trim()) {
        this.error = "Enter your email address.";
        return;
      }
      this.message = "Email format looks good. No reset link was sent in this UI preview.";
    },

    checkResetPassword() {
      this.clearFeedback();
      if (!this.email.trim() || !this.resetToken.trim()) {
        this.error = "Enter your email and reset token.";
        return;
      }
      if (!strongPassword(this.newPassword)) {
        this.error = "New password needs 8–128 characters, uppercase, lowercase and a number.";
        return;
      }
      if (this.newPassword !== this.confirmNewPassword) {
        this.error = "New passwords do not match.";
        return;
      }
      this.message = "Form looks good. The password was not changed in this UI preview.";
    }
  }
}).mount("#app");
