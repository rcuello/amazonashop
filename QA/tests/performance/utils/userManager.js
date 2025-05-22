export class UserManager {
  constructor(users) {
    this.users = users;
    this.validateUsers();
  }

  getRandomUser() {
    const user = this.users[Math.floor(Math.random() * this.users.length)];
    if (!user.email || !user.password) {
      throw new Error(`Usuario inválido: ${JSON.stringify(user)}`);
    }
    return user;
  }

  getUserByIndex(index) {
    return this.users[index % this.users.length];
  }

  getUserCount() {
    return this.users.length;
  }

  validateUsers() {
    if (!this.users?.length) {
      throw new Error('No hay usuarios disponibles para la prueba');
    }
  }
}