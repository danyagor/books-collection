﻿using System;
using System.Data;
using System.Collections.Generic;
using System.Windows.Forms;
using CollectionsProject.CustomComponents;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Drawing2D;

namespace CollectionsProject.Forms
{
    public partial class ItemPropertiesForm : Form
    {
        MainForm mf;
        int typeId;
        string collectionName;
        string foreignTableName;
        int itemId;

        List<TextField> textFields;
        Field[] fields;

        Image[] images;
        string[] comments;

        #region Конструкторы

        // Конструктор для добавления предмета
        public ItemPropertiesForm(MainForm mf, int typeId, string collectionName, string foreignTableName = "")
        {
            InitializeComponent();

            this.mf = mf;
            this.typeId = typeId;
            this.collectionName = collectionName;
            this.foreignTableName = foreignTableName;

            images = new Image[4];
            comments = new string[4];
            textFields = new List<TextField>();
            if (foreignTableName == "")
                fields = CollectionTypes.GetCollection(typeId)[0].Fields;
            else
            {
                fields = CollectionTypes.GetCollection(typeId)[foreignTableName].Fields;
                tabControl.TabPages.RemoveAt(2);
            }

            AddTextFields();
            btnEditItem.Text = "Добавить предмет в коллекцию";
        }

        // Конструктор для изменения предмета
        public ItemPropertiesForm(MainForm mf, int typeId, string collectionName, int itemId, string foreignTableName = "")
        {
            InitializeComponent();

            this.mf = mf;
            this.typeId = typeId;
            this.collectionName = collectionName;
            this.itemId = itemId;
            this.foreignTableName = foreignTableName;

            images = new Image[4];
            comments = new string[4];
            textFields = new List<TextField>();
            if (foreignTableName == "")
                fields = CollectionTypes.GetCollection(typeId)[0].Fields;
            else
            {
                fields = CollectionTypes.GetCollection(typeId)[foreignTableName].Fields;
                tabControl.TabPages.RemoveAt(2); // Удаление вкладки фотографий
            }

            AddTextFields();
            InsertDataInTextFields();
            btnEditItem.Text = "Изменить данные о предмете";
        }

        #endregion Конструкторы



        #region Методы

        // Создание текстовых полей
        private void AddTextFields()
        {
            foreach (Field field in fields)
            {
                string reqStr = "";
                if (field.RequiredField)
                    reqStr = "*";

                // Внешнее поле
                if (field.ForeignKey)
                {
                    // Получение во внешней таблице значений и вывод в текущий ComboBox
                    TextField tf = new TextField(field.ProgramName + reqStr, field.BaseName, true);
                    DataTable dt = new DataTable();
                    dt = mf.CurrentDatabase.GetNameFields(typeId, field.ForeignTable);
                    //dt.Rows.Add("-1", "Добавить новый элемент...");
                    tf.CB.DataSource = dt;
                    tf.CB.ValueMember = "id";
                    tf.CB.DisplayMember = "data";
                    tf.CB.DropDownStyle = ComboBoxStyle.DropDownList;
                    tf.CB.SelectedValueChanged += CB_SelectedValueChanged;
                    if (tf.CB.Items.Count != 0)
                        tf.CB.SelectedIndex = 0;
                    tf.Width = flowLayoutPanel.Width - 8;
                    flowLayoutPanel.Controls.Add(tf);
                    textFields.Add(tf);
                }
                else // Обычное поле
                {
                    TextField tf = new TextField(field.ProgramName + reqStr, field.BaseName, false);
                    tf.Width = flowLayoutPanel.Width - 8;
                    flowLayoutPanel.Controls.Add(tf);
                    textFields.Add(tf);
                }
            }
        }
        
        // Вставление данных о предмете
        private void InsertDataInTextFields()
        {
            if (itemId != 0)
            {
                DataRow itemData;
                if (foreignTableName == "")
                    itemData = mf.CurrentDatabase.GetItemFromCollection(typeId, itemId, collectionName);
                else
                    itemData = mf.CurrentDatabase.GetItemFromCollection(typeId, itemId, "", foreignTableName);

                int textFieldsCounter = 0;
                for (int i = 0; i < itemData.ItemArray.Length; i++)
                {
                    if (itemData.Table.Columns[i].ColumnName != "id" &&
                        itemData.Table.Columns[i].ColumnName != "uploadDate" &&
                        itemData.Table.Columns[i].ColumnName != "changeDate" &&
                        itemData.Table.Columns[i].ColumnName != "note" &&
                        itemData.Table.Columns[i].ColumnName != "photo1" &&
                        itemData.Table.Columns[i].ColumnName != "photo2" &&
                        itemData.Table.Columns[i].ColumnName != "photo3" &&
                        itemData.Table.Columns[i].ColumnName != "photo4" &&
                        itemData.Table.Columns[i].ColumnName != "comment1" &&
                        itemData.Table.Columns[i].ColumnName != "comment2" &&
                        itemData.Table.Columns[i].ColumnName != "comment3" &&
                        itemData.Table.Columns[i].ColumnName != "comment4")
                    {
                        if (fields[textFieldsCounter].ForeignKey)
                            textFields[textFieldsCounter].CB.SelectedValue = itemData.ItemArray[i];
                        else
                            textFields[textFieldsCounter].Value = itemData.ItemArray[i].ToString();

                        textFieldsCounter++;
                    }
                }

                // Получение дополнительной информации о предмете (описание, фотографии, комментарии к фотографиям)
                for (int i = 0; i < itemData.Table.Columns.Count; i++)
                {
                    // Описание
                    if (itemData.Table.Columns[i].ColumnName == "note")
                        tbNote.Text = itemData.ItemArray[i].ToString();

                    // Фотографии
                    for (int j = 0; j < 4; j++)
                        if (itemData.Table.Columns[i].ColumnName == "photo" + (j + 1))
                            if (itemData.ItemArray[i].ToString() != "")
                                images[j] = ByteToImage((byte[])itemData.ItemArray[i]);

                    // Комментарии
                    for (int j = 0; j < 4; j++)
                        if (itemData.Table.Columns[i].ColumnName == "comment" + (j + 1))
                            comments[j] = itemData.ItemArray[i].ToString();
                }
            }
        }

        // Редактирование предмета
        private void EditItem()
        {
            string[] userText = new string[textFields.Count];

            // Проверка на введенность всех обязательных полей
            bool requiredCheck = true;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].RequiredField && textFields[i].Value == "")
                {
                    requiredCheck = false;
                    break;
                }
            }

            if (requiredCheck)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (textFields[i].Identificated)
                    {
                        if (textFields[i].CB.Items.Count != 0)
                            userText[i] = textFields[i].CB.SelectedValue.ToString();
                    }
                    else
                        userText[i] = textFields[i].Value;
                }

                if (itemId == 0) // Добавление
                {
                    if (foreignTableName == "")
                        mf.CurrentDatabase.AddItem(typeId, userText, tbNote.Text, collectionName, "", GetItemImages());
                    else
                        mf.CurrentDatabase.AddItem(typeId, userText, tbNote.Text, "", foreignTableName);
                }
                else // Обновление
                {
                    if (foreignTableName == "")
                        mf.CurrentDatabase.UpdateItem(typeId, itemId, userText, tbNote.Text, collectionName, "", GetItemImages());
                    else
                        mf.CurrentDatabase.UpdateItem(typeId, itemId, userText, tbNote.Text, "", foreignTableName);
                }

                Close();
            }
            else
                MessageBox.Show("Введены не все обязательные поля");
        }

        #endregion Методы



        #region Вспомогательные методы

        // Конвертирует массив байтов в картинку
        public Image ByteToImage(byte[] imageBytes)
        {
            if (imageBytes.Length == 0)
                return null;

            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = new Bitmap(ms);
            return image;
        }

        // Возвращает информацию о картинках
        private ItemImage[] GetItemImages()
        {
            List<ItemImage> itemImages = new List<ItemImage>();
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    Image resizedImage = ResizeImage(images[i], 700, 700);
                    MemoryStream ms = new MemoryStream();
                    resizedImage.Save(ms, ImageFormat.Jpeg);
                    byte[] bytes = ms.ToArray();
                    itemImages.Add(new ItemImage(bytes, comments[i]));
                }
                else
                    itemImages.Add(new ItemImage(null, ""));
            }

            return itemImages.ToArray();
        }

        // Смена размера изображения
        public Image ResizeImage(Image image, int nWidth, int nHeight)
        {
            int newWidth, newHeight;
            var coefH = (double)nHeight / (double)image.Height;
            var coefW = (double)nWidth / (double)image.Width;
            if (coefW >= coefH)
            {
                newHeight = (int)(image.Height * coefH);
                newWidth = (int)(image.Width * coefH);
            }
            else
            {
                newHeight = (int)(image.Height * coefW);
                newWidth = (int)(image.Width * coefW);
            }

            Image result = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(result))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                g.DrawImage(image, 0, 0, newWidth, newHeight);
                g.Dispose();
            }
            return result;
        }

        #endregion Вспомогательные методы



        #region События

        // Редактирование предмета
        private void btnEditItem_Click(object sender, EventArgs e)
        {
            EditItem();
        }

        // Смена картинки и его комменария
        private void lbPhotos_SelectedIndexChanged(object sender, EventArgs e)
        {
            pbPhoto.Image = images[lbPhotos.SelectedIndex];
            tbComment.Text = comments[lbPhotos.SelectedIndex];
        }

        // Клик на кнопку "Назначить"
        private void btnAssign_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Файлы изображений| *.jpg; *.jpeg; *.png";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                images[lbPhotos.SelectedIndex] = Image.FromFile(ofd.FileName);
                pbPhoto.Image = images[lbPhotos.SelectedIndex];
            }
        }

        // Клик на кнопку "Очистить"
        private void btnClear_Click(object sender, EventArgs e)
        {
            images[lbPhotos.SelectedIndex] = null;
            pbPhoto.Image = null;

            comments[lbPhotos.SelectedIndex] = "";
            tbComment.Text = "";
        }

        // Смена фокуса с поля с комментарием
        private void tbComment_Leave(object sender, EventArgs e)
        {
            comments[lbPhotos.SelectedIndex] = tbComment.Text;
        }

        // Смена вкладки
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab.Text == "Фотографии")
                lbPhotos.SelectedIndex = 0;
        }

        // UNDONE: Клик на "Добавить новый предмет..."
        private void CB_SelectedValueChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            //if (cb.SelectedValue.ToString() == "-1")
            //{
            //    ItemPropertiesForm ifp = new ItemPropertiesForm(mf, typeId, collectionName, )
            //}
        }

        #endregion События

        private void flowLayoutPanel_Resize(object sender, EventArgs e)
        {
            for (int i = 0; i < textFields.Count; i++)
                textFields[i].Width = flowLayoutPanel.Width - 8;
        }
    }
}
